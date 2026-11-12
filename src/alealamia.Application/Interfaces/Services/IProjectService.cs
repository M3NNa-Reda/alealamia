using alealamia.Application.DTOs.Projects;
using alealamia.Domain.Enums;

namespace alealamia.Application.Interfaces.Services
{
    public interface IProjectService
    {
        Task<ProjectDetailsDto?> GetDetailsAsync(int id);
        Task<IEnumerable<ProjectDto>> GetByClientAsync(int clientProfileId);
        Task<IEnumerable<ProjectDto>> GetByStatusAsync(ProjectStatus status);
        Task<ProjectDto> CreateAsync(CreateProjectDto dto);
        Task<ProjectDto> UpdateAsync(int id, UpdateProjectDto dto);
        Task<ProjectServiceDto> AddServiceAsync(int projectId, AddProjectServiceDto dto);
        Task<ProjectServiceDto> UpdateServiceAsync(int projectId, int serviceId, UpdateProjectServiceDto dto);
        Task RemoveServiceAsync(int projectId, int serviceId);
    }
}