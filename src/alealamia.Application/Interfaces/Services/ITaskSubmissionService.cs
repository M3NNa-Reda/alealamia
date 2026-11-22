using alealamia.Application.DTOs.TaskSubmission;

namespace alealamia.Application.Interfaces.Services
{
    public interface ITaskSubmissionService
    {
        Task<TaskSubmissionDetailsDto> CreateSubmissionAsync(CreateTaskSubmissionDto submission);

        Task<TaskSubmissionDetailsDto?> GetByIdAsync(int id);

        Task<IEnumerable<TaskSubmissionDto>> GetByTaskIdAsync(int taskId);

        Task<IEnumerable<TaskSubmissionDto>> GetByFreelancerProfileIdAsync(int freelancerProfileId);
    }
}