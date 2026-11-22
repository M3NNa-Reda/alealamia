using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace alealamia.Infrastructure.Repositories;

public class TaskOfferRepository : GenericRepository<TaskOffer>, ITaskOfferRepository
{
    public TaskOfferRepository(ApplicationDbContext context) : base(context) { }

    public async Task<TaskOffer?> GetDetailsByIdAsync(int id)
    {
        return await _context.TaskOffers.AsNoTracking()
            .Include(o => o.Task)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IReadOnlyList<TaskOffer>> GetByTaskIdAsync(int taskId)
    {
        return await _context.TaskOffers.AsNoTracking()
            .Where(o => o.TaskId == taskId)
            .OrderBy(o => o.SentAt)
            .ThenBy(o => o.Id)
            .ToListAsync();
    }

    public async Task<IReadOnlySet<int>> GetOfferedFreelancerIdsAsync(int taskId)
    {
        var ids = await _context.TaskOffers.AsNoTracking()
            .Where(o => o.TaskId == taskId)
            .Select(o => o.FreelancerProfileId)
            .ToListAsync();

        return ids.ToHashSet();
    }

    public async Task<(IReadOnlyList<TaskOffer> Items, int TotalCount)> SearchAsync(
        int? freelancerProfileId,
        int? taskId,
        TaskOfferStatus? status,
        int page,
        int pageSize)
    {
        var query = _context.TaskOffers.AsNoTracking().Include(o => o.Task).AsQueryable();

        if (freelancerProfileId.HasValue)
            query = query.Where(o => o.FreelancerProfileId == freelancerProfileId.Value);
        if (taskId.HasValue)
            query = query.Where(o => o.TaskId == taskId.Value);
        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.SentAt)
            .ThenByDescending(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<int> CountAcceptedSinceAsync(int freelancerProfileId, DateTime sinceUtc)
    {
        return await _context.TaskOffers.AsNoTracking()
            .CountAsync(o => o.FreelancerProfileId == freelancerProfileId
                             && o.Status == TaskOfferStatus.Accepted
                             && o.RespondedAt >= sinceUtc);
    }

    public async Task<IReadOnlyDictionary<int, int>> GetAcceptedCountsSinceAsync(
        IReadOnlyCollection<int> freelancerProfileIds,
        DateTime sinceUtc)
    {
        var ids = freelancerProfileIds.ToList();

        return await _context.TaskOffers.AsNoTracking()
            .Where(o => ids.Contains(o.FreelancerProfileId)
                        && o.Status == TaskOfferStatus.Accepted
                        && o.RespondedAt >= sinceUtc)
            .GroupBy(o => o.FreelancerProfileId)
            .Select(g => new { FreelancerProfileId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.FreelancerProfileId, x => x.Count);
    }

    public async Task<bool> TryAcceptAsync(int offerId, DateTime utcNow)
    {
        var affected = await _context.TaskOffers
            .Where(o => o.Id == offerId && o.Status == TaskOfferStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, TaskOfferStatus.Accepted)
                .SetProperty(o => o.RespondedAt, (DateTime?)utcNow));

        return affected == 1;
    }

    public async Task<bool> TryDeclineAsync(int offerId, DateTime utcNow)
    {
        var affected = await _context.TaskOffers
            .Where(o => o.Id == offerId && o.Status == TaskOfferStatus.Pending)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, TaskOfferStatus.Declined)
                .SetProperty(o => o.RespondedAt, (DateTime?)utcNow));

        return affected == 1;
    }

    public async Task<int> ClosePendingAsync(int taskId, int? exceptOfferId, DateTime utcNow)
    {
        var excluded = exceptOfferId ?? 0;

        return await _context.TaskOffers
            .Where(o => o.TaskId == taskId
                        && o.Status == TaskOfferStatus.Pending
                        && o.Id != excluded)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.Status, TaskOfferStatus.Closed)
                .SetProperty(o => o.RespondedAt, (DateTime?)utcNow));
    }

    public async Task<TaskOffer> GetOrCreateAcceptedAsync(int taskId, int freelancerProfileId, DateTime utcNow)
    {
        var offer = await _context.TaskOffers
            .FirstOrDefaultAsync(o => o.TaskId == taskId && o.FreelancerProfileId == freelancerProfileId);

        if (offer is null)
        {
            offer = new TaskOffer
            {
                TaskId = taskId,
                FreelancerProfileId = freelancerProfileId,
                Status = TaskOfferStatus.Accepted,
                SentAt = utcNow,
                RespondedAt = utcNow
            };
            await _context.TaskOffers.AddAsync(offer);
            return offer;
        }

        offer.Status = TaskOfferStatus.Accepted;
        offer.RespondedAt = utcNow;
        return offer;
    }

    public async Task AddRangeAsync(IEnumerable<TaskOffer> offers)
    {
        await _context.TaskOffers.AddRangeAsync(offers);
    }
}
