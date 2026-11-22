using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Tasks;

namespace alealamia.Application.Interfaces.Services;

public interface ITaskService
{
    Task<TaskDetailsDto?> GetDetailsAsync(int id);
    Task<PagedResultDto<TaskDto>> SearchAsync(TaskFilterDto filter);
    Task<IEnumerable<TaskDto>> GetByProjectAsync(int projectId);
    Task<TaskDto> CreateAsync(CreateTaskDto dto);
    Task<TaskDto> UpdateAsync(int id, UpdateTaskDto dto);
    Task<IEnumerable<TaskRequiredSkillDto>> UpdateRequiredSkillsAsync(int taskId, UpdateTaskRequiredSkillsDto dto);
    Task<TaskDto> UpdateStatusAsync(int taskId, UpdateTaskStatusDto dto);
    Task<IEnumerable<TaskStatusDto>> GetStatusHistoryAsync(int taskId);
    Task<IEnumerable<TaskFileDto>> GetFilesAsync(int taskId);
    Task<TaskFileDto> AddFileAsync(int taskId, AddTaskFileDto dto);
    Task RemoveFileAsync(int taskId, int fileId);
}
