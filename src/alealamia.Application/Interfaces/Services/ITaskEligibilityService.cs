using alealamia.Application.DTOs.TaskAssignments;

namespace alealamia.Application.Interfaces.Services;

public interface ITaskEligibilityService
{
    Task<TaskEligibilityDto> CheckAsync(int taskId, int freelancerProfileId);
    Task<IEnumerable<EligibleFreelancerDto>> GetEligibleFreelancersAsync(int taskId);
    Task<FreelancerTaskLimitDto> GetMonthlyLimitAsync(int freelancerProfileId);
}
