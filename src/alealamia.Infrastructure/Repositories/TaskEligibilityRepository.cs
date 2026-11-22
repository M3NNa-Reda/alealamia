using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories;

public class TaskEligibilityRepository : ITaskEligibilityRepository
{
    private readonly ApplicationDbContext _context;

    public TaskEligibilityRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<FreelancerEligibilitySnapshot?> GetFreelancerSnapshotAsync(int freelancerProfileId, DateTime utcNow)
    {
        return await Project(_context.FreelancerProfiles.AsNoTracking().Where(f => f.Id == freelancerProfileId), utcNow)
            .FirstOrDefaultAsync();
    }

    public async Task<IReadOnlyList<FreelancerEligibilitySnapshot>> GetCandidatesAsync(int trackId, DateTime utcNow)
    {
        var query = _context.FreelancerProfiles.AsNoTracking()
            .Where(f => f.TrackId == trackId
                        && f.ApprovalStatus == FreelancerApprovalStatus.Approved
                        && f.IsAvailable
                        && !f.FreelancerBlocks.Any(b => b.StartedAt <= utcNow && b.EndsAt > utcNow));

        return await Project(query, utcNow).ToListAsync();
    }

    private static IQueryable<FreelancerEligibilitySnapshot> Project(IQueryable<FreelancerProfile> query, DateTime utcNow)
    {
        return query.Select(f => new FreelancerEligibilitySnapshot
        {
            FreelancerProfileId = f.Id,
            UserId = f.UserId,
            TrackId = f.TrackId,
            ApprovalStatus = f.ApprovalStatus,
            IsAvailable = f.IsAvailable,
            SkillIds = f.FreelancerSkills.Select(s => s.SkillId).ToList(),
            ActiveBlockEndsAt = f.FreelancerBlocks
                .Where(b => b.StartedAt <= utcNow && b.EndsAt > utcNow)
                .Max(b => (DateTime?)b.EndsAt)
        });
    }
}
