using alealamia.Application.DTOs.Tasks;

namespace alealamia.Application.Interfaces.Services;

public interface ITaskDeadlineService
{
    Task<TaskDeadlineDto> GetAsync(int taskId);
    Task<IEnumerable<TaskDeadlineDto>> GetOverdueAsync();
    Task<IEnumerable<TaskDeadlineDto>> GetDueSoonAsync(int withinHours);
    Task<TaskDeadlineDto> ExtendAsync(int taskId, ExtendTaskDeadlineDto dto);
    Task<TaskDeadlineDto> RecordActualDurationAsync(int taskId);
}
