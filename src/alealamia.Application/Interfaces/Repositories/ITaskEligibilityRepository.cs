using alealamia.Application.DTOs.TaskAssignments;

namespace alealamia.Application.Interfaces.Repositories;

public interface ITaskEligibilityRepository
{
    Task<FreelancerEligibilitySnapshot?> GetFreelancerSnapshotAsync(int freelancerProfileId, DateTime utcNow);
    Task<IReadOnlyList<FreelancerEligibilitySnapshot>> GetCandidatesAsync(int trackId, DateTime utcNow);
}
