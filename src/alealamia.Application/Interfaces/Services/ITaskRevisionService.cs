using alealamia.Application.DTOs.TaskRevision;

namespace alealamia.Application.Interfaces.Services
{
    public interface ITaskRevisionService
    {
        Task<TaskRevisionDto> CreateRevisionAsync(CreateTaskRevisionDto revision);
        Task<TaskRevisionDto?> GetByIdAsync(int id);
        Task<IEnumerable<TaskRevisionDto>> GetByTaskIdAsync(int taskId);
        Task<IEnumerable<TaskRevisionDto>> GetByTaskReviewIdAsync(int taskReviewId);
        Task<TaskRevisionDto?> CompleteRevisionAsync(int revisionId);
    }
}
