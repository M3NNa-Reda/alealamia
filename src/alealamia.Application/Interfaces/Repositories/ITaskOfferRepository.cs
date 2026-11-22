using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using Task = System.Threading.Tasks.Task;

namespace alealamia.Application.Interfaces.Repositories;

public interface ITaskOfferRepository : IGenericRepository<TaskOffer>
{
    Task<TaskOffer?> GetDetailsByIdAsync(int id);
    Task<IReadOnlyList<TaskOffer>> GetByTaskIdAsync(int taskId);
    Task<IReadOnlySet<int>> GetOfferedFreelancerIdsAsync(int taskId);
    Task<(IReadOnlyList<TaskOffer> Items, int TotalCount)> SearchAsync(
        int? freelancerProfileId,
        int? taskId,
        TaskOfferStatus? status,
        int page,
        int pageSize);
    Task<int> CountAcceptedSinceAsync(int freelancerProfileId, DateTime sinceUtc);
    Task<IReadOnlyDictionary<int, int>> GetAcceptedCountsSinceAsync(IReadOnlyCollection<int> freelancerProfileIds, DateTime sinceUtc);
    Task<bool> TryAcceptAsync(int offerId, DateTime utcNow);
    Task<bool> TryDeclineAsync(int offerId, DateTime utcNow);
    Task<int> ClosePendingAsync(int taskId, int? exceptOfferId, DateTime utcNow);
    Task<TaskOffer> GetOrCreateAcceptedAsync(int taskId, int freelancerProfileId, DateTime utcNow);
    Task AddRangeAsync(IEnumerable<TaskOffer> offers);
}
