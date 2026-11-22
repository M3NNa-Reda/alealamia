using alealamia.Domain.Entities;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface ISubmissionFileRepository
    {
        Task<IEnumerable<SubmissionFile>> GetByTaskSubmissionIdAsync(int id);
        Task<IEnumerable<SubmissionFile>> GetByFileIdAsync(int id);
        Task<IEnumerable<SubmissionFile>> GetByUploadedAtRangeAsync(DateTime fromDate, DateTime toDate);
        Task<SubmissionFile?> GetByTaskSubmissionAndFileIdAsync(int taskSubmissionId,int fileId);

    }
}
