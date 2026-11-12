using alealamia.Application.DTOs.Projects;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using ProjectServiceEntity = alealamia.Domain.Entities.ProjectService;
using Task = System.Threading.Tasks.Task;

namespace alealamia.Application.Services
{
    public class ProjectService : IProjectService
    {
        private readonly IProjectRepository _projectRepository;
        private readonly IProjectServiceRepository _projectServiceRepository;
        private readonly IServiceRepository _serviceRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProjectService(
            IProjectRepository projectRepository,
            IProjectServiceRepository projectServiceRepository,
            IServiceRepository serviceRepository,
            IUnitOfWork unitOfWork)
        {
            _projectRepository = projectRepository;
            _projectServiceRepository = projectServiceRepository;
            _serviceRepository = serviceRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ProjectDetailsDto?> GetDetailsAsync(int id)
        {
            var project = await _projectRepository.GetDetailsByIdAsync(id);
            return project is null ? null : MapToDetailsDto(project);
        }

        public async Task<IEnumerable<ProjectDto>> GetByClientAsync(int clientProfileId)
        {
            var projects = await _projectRepository.GetByClientProfileIdAsync(clientProfileId);
            return projects.Select(MapToDto);
        }

        public async Task<IEnumerable<ProjectDto>> GetByStatusAsync(ProjectStatus status)
        {
            var projects = await _projectRepository.GetByStatusAsync(status);
            return projects.Select(MapToDto);
        }

        public async Task<ProjectDto> CreateAsync(CreateProjectDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Project title is required.");
            if (string.IsNullOrWhiteSpace(dto.Description))
                throw new ArgumentException("Project description is required.");
            if (string.IsNullOrWhiteSpace(dto.Currency) || dto.Currency.Trim().Length != 3)
                throw new ArgumentException("Currency must be a 3-letter code (e.g. EGP).");
            if (dto.Services.Count == 0)
                throw new ArgumentException("At least one service must be selected.");
            if (dto.Services.GroupBy(s => s.ServiceId).Any(g => g.Count() > 1))
                throw new ArgumentException("The same service cannot be selected twice.");

            var project = new Project
            {
                ClientProfileId = dto.ClientProfileId,
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                HasCustomization = dto.HasCustomization,
                Status = ProjectStatus.Pending,
                FinalPrice = null,   
                Currency = dto.Currency.Trim().ToUpperInvariant(),
                CreatedAt = DateTime.UtcNow
            };

            foreach (var item in dto.Services)
            {
                var service = await _serviceRepository.GetByIdAsync(item.ServiceId);
                if (service is null || !service.IsActive)
                    throw new InvalidOperationException($"Service {item.ServiceId} is not available.");

                project.ProjectServices.Add(new ProjectServiceEntity
                {
                    ServiceId = item.ServiceId,
                    CustomizationDetails = item.CustomizationDetails,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await _projectRepository.AddAsync(project);
            await _unitOfWork.CompleteAsync();
            return MapToDto(project);
        }

        public async Task<ProjectDto> UpdateAsync(int id, UpdateProjectDto dto)
        {
            var project = await GetProjectOrThrowAsync(id);
            EnsureEditable(project);

            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("Project title is required.");
            if (string.IsNullOrWhiteSpace(dto.Description))
                throw new ArgumentException("Project description is required.");

            project.Title = dto.Title.Trim();
            project.Description = dto.Description.Trim();
            project.HasCustomization = dto.HasCustomization;
            project.UpdatedAt = DateTime.UtcNow;

            _projectRepository.Update(project);
            await _unitOfWork.CompleteAsync();
            return MapToDto(project);
        }

        public async Task<ProjectServiceDto> AddServiceAsync(int projectId, AddProjectServiceDto dto)
        {
            var project = await GetProjectOrThrowAsync(projectId);
            EnsureEditable(project);

            var service = await _serviceRepository.GetByIdAsync(dto.ServiceId);
            if (service is null || !service.IsActive)
                throw new InvalidOperationException($"Service {dto.ServiceId} is not available.");

            var existing = await _projectServiceRepository.GetByProjectAndServiceAsync(projectId, dto.ServiceId);
            if (existing is not null)
                throw new InvalidOperationException("This service is already added to the project.");

            var projectService = new ProjectServiceEntity
            {
                ProjectId = projectId,
                ServiceId = dto.ServiceId,
                CustomizationDetails = dto.CustomizationDetails,
                CreatedAt = DateTime.UtcNow
            };

            await _projectServiceRepository.AddAsync(projectService);
            await _unitOfWork.CompleteAsync();
            return MapServiceDto(projectService, service.Name);
        }

        public async Task<ProjectServiceDto> UpdateServiceAsync(int projectId, int serviceId, UpdateProjectServiceDto dto)
        {
            var project = await GetProjectOrThrowAsync(projectId);
            EnsureEditable(project);

            var projectService = await _projectServiceRepository.GetByProjectAndServiceAsync(projectId, serviceId)
                ?? throw new KeyNotFoundException("The service is not part of this project.");

            projectService.CustomizationDetails = dto.CustomizationDetails;

            _projectServiceRepository.Update(projectService);
            await _unitOfWork.CompleteAsync();

            var service = await _serviceRepository.GetByIdAsync(serviceId);
            return MapServiceDto(projectService, service?.Name ?? string.Empty);
        }

        public async Task RemoveServiceAsync(int projectId, int serviceId)
        {
            var project = await GetProjectOrThrowAsync(projectId);
            EnsureEditable(project);

            var projectService = await _projectServiceRepository.GetByProjectAndServiceAsync(projectId, serviceId)
                ?? throw new KeyNotFoundException("The service is not part of this project.");

            var all = await _projectServiceRepository.GetByProjectIdAsync(projectId);
            if (all.Count() <= 1)
                throw new InvalidOperationException("A project must keep at least one service.");

            _projectServiceRepository.Delete(projectService);
            await _unitOfWork.CompleteAsync();
        }

        // ---------- helpers ----------

        private async Task<Project> GetProjectOrThrowAsync(int id)
        {
            return await _projectRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Project {id} was not found.");
        }

        private static void EnsureEditable(Project project)
        {
            if (project.Status != ProjectStatus.Pending && project.Status != ProjectStatus.UnderReview)
                throw new InvalidOperationException("The project can no longer be modified in its current status.");
        }

        private static ProjectServiceDto MapServiceDto(ProjectServiceEntity ps, string serviceName) => new()
        {
            Id = ps.Id,
            ProjectId = ps.ProjectId,
            ServiceId = ps.ServiceId,
            ServiceName = serviceName,
            CustomizationDetails = ps.CustomizationDetails,
            CreatedAt = ps.CreatedAt
        };

        private static ProjectDto MapToDto(Project p) => new()
        {
            Id = p.Id,
            ClientProfileId = p.ClientProfileId,
            Title = p.Title,
            Description = p.Description,
            HasCustomization = p.HasCustomization,
            Status = p.Status,
            FinalPrice = p.FinalPrice,
            Currency = p.Currency,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        };

        private static ProjectDetailsDto MapToDetailsDto(Project p) => new()
        {
            Id = p.Id,
            ClientProfileId = p.ClientProfileId,
            Title = p.Title,
            Description = p.Description,
            HasCustomization = p.HasCustomization,
            Status = p.Status,
            FinalPrice = p.FinalPrice,
            Currency = p.Currency,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Services = p.ProjectServices
                .Select(ps => MapServiceDto(ps, ps.Service?.Name ?? string.Empty))
                .ToList(),
            StatusHistory = p.StatusHistory
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => new ProjectStatusHistoryDto
                {
                    Id = h.Id,
                    ProjectId = h.ProjectId,
                    OldStatus = h.OldStatus,
                    NewStatus = h.NewStatus,
                    ChangedByUserId = h.ChangedByUserId,
                    Notes = h.Notes,
                    ChangedAt = h.ChangedAt
                })
                .ToList()
        };
    }
}