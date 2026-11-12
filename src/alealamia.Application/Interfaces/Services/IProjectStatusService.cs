using alealamia.Application.DTOs.Projects;

namespace alealamia.Application.Interfaces.Services
{
    public interface IProjectStatusService
    {
        Task<ProjectDto> UpdateStatusAsync(int projectId, UpdateProjectStatusDto dto);
        Task<IEnumerable<ProjectStatusHistoryDto>> GetHistoryAsync(int projectId);
    }
}