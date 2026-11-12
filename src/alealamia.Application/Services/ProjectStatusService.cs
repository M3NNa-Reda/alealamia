using alealamia.Application.DTOs.Projects;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;

namespace alealamia.Application.Services
{
    public class ProjectStatusService : IProjectStatusService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectStatusHistoryRepository _historyRepository;
        private readonly IUnitOfWork _unitOfWork;

        
        private static readonly Dictionary<ProjectStatus, ProjectStatus[]> AllowedTransitions = new()
        {
            [ProjectStatus.Pending] = new[] { ProjectStatus.UnderReview, ProjectStatus.Cancelled },
            [ProjectStatus.UnderReview] = new[] { ProjectStatus.PriceSet, ProjectStatus.Cancelled },
            [ProjectStatus.PriceSet] = new[] { ProjectStatus.InProgress, ProjectStatus.Cancelled },
            [ProjectStatus.InProgress] = new[] { ProjectStatus.Completed, ProjectStatus.Cancelled },
            [ProjectStatus.Completed] = Array.Empty<ProjectStatus>(),
            [ProjectStatus.Cancelled] = Array.Empty<ProjectStatus>()
        };

        public ProjectStatusService(
            IProjectRepository projectRepository,
            IProjectStatusHistoryRepository historyRepository,
            IUnitOfWork unitOfWork)
        {
            _projectRepository = projectRepository;
            _historyRepository = historyRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ProjectDto> UpdateStatusAsync(int projectId, UpdateProjectStatusDto dto)
        {
            var project = await _projectRepository.GetByIdAsync(projectId)
                ?? throw new KeyNotFoundException($"Project {projectId} was not found.");

            if (string.IsNullOrWhiteSpace(dto.ChangedByUserId))
                throw new ArgumentException("ChangedByUserId is required.");

            var oldStatus = project.Status;
            if (dto.NewStatus == oldStatus)
                throw new InvalidOperationException("The project is already in this status.");

            if (!AllowedTransitions[oldStatus].Contains(dto.NewStatus))
                throw new InvalidOperationException($"Cannot move a project from {oldStatus} to {dto.NewStatus}.");

            if (dto.NewStatus == ProjectStatus.PriceSet)
            {
                if (dto.FinalPrice is null || dto.FinalPrice <= 0)
                    throw new ArgumentException("A positive final price is required to set the price.");

                project.FinalPrice = dto.FinalPrice;
            }

            if (dto.NewStatus == ProjectStatus.InProgress && project.FinalPrice is null)
                throw new InvalidOperationException("The final price must be set before starting the project.");

            project.Status = dto.NewStatus;
            project.UpdatedAt = DateTime.UtcNow;

            var history = new ProjectStatusHistory
            {
                ProjectId = projectId,
                OldStatus = oldStatus,
                NewStatus = dto.NewStatus,
                ChangedByUserId = dto.ChangedByUserId,
                Notes = dto.Notes,
                ChangedAt = DateTime.UtcNow
            };

            _projectRepository.Update(project);
            await _historyRepository.AddAsync(history);
            await _unitOfWork.CompleteAsync();   

            return new ProjectDto
            {
                Id = project.Id,
                ClientProfileId = project.ClientProfileId,
                Title = project.Title,
                Description = project.Description,
                HasCustomization = project.HasCustomization,
                Status = project.Status,
                FinalPrice = project.FinalPrice,
                Currency = project.Currency,
                CreatedAt = project.CreatedAt,
                UpdatedAt = project.UpdatedAt
            };
        }

        public async Task<IEnumerable<ProjectStatusHistoryDto>> GetHistoryAsync(int projectId)
        {
            var history = await _historyRepository.GetByProjectIdAsync(projectId);

            return history.Select(h => new ProjectStatusHistoryDto
            {
                Id = h.Id,
                ProjectId = h.ProjectId,
                OldStatus = h.OldStatus,
                NewStatus = h.NewStatus,
                ChangedByUserId = h.ChangedByUserId,
                Notes = h.Notes,
                ChangedAt = h.ChangedAt
            });
        }
    }
}