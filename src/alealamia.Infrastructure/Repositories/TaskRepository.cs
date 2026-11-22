using alealamia.Application.Interfaces.Repositories;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using TaskEntity = alealamia.Domain.Entities.Task;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Infrastructure.Repositories;

public class TaskRepository : GenericRepository<TaskEntity>, ITaskRepository
{
    public TaskRepository(ApplicationDbContext context) : base(context) { }

    public async Task<TaskEntity?> GetReadOnlyByIdAsync(int id)
    {
        return await _context.Tasks.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<TaskEntity?> GetDetailsByIdAsync(int id)
    {
        return await _context.Tasks.AsNoTracking()
            .Include(t => t.Project)
            .Include(t => t.TaskRequiredSkills).ThenInclude(s => s.Skill)
            .Include(t => t.TaskFiles).ThenInclude(f => f.File)
            .Include(t => t.TaskStatusHistories)
            .Include(t => t.TaskReassignments)
            .AsSplitQuery()
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IReadOnlyList<TaskEntity>> GetByProjectIdAsync(int projectId)
    {
        return await _context.Tasks.AsNoTracking()
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.PublishedDeadline)
            .ThenBy(t => t.Id)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TaskEntity>> GetByFreelancerIdAsync(int freelancerProfileId)
    {
        return await _context.Tasks.AsNoTracking()
            .Where(t => t.AssignedFreelancerId == freelancerProfileId)
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .ToListAsync();
    }

    public async Task<(IReadOnlyList<TaskEntity> Items, int TotalCount)> SearchAsync(
        TaskStatus? status,
        int? projectId,
        int? trackId,
        int? assignedFreelancerId,
        int page,
        int pageSize)
    {
        var query = _context.Tasks.AsNoTracking().AsQueryable();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);
        if (projectId.HasValue)
            query = query.Where(t => t.ProjectId == projectId.Value);
        if (trackId.HasValue)
            query = query.Where(t => t.TrackId == trackId.Value);
        if (assignedFreelancerId.HasValue)
            query = query.Where(t => t.AssignedFreelancerId == assignedFreelancerId.Value);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .ThenByDescending(t => t.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<IReadOnlyList<TaskEntity>> GetOverdueAsync(DateTime utcNow)
    {
        return await _context.Tasks.AsNoTracking()
            .Where(t => (t.Status == TaskStatus.Assigned || t.Status == TaskStatus.RevisionRequested)
                        && (t.ExtensionDeadline ?? t.PublishedDeadline) < utcNow)
            .OrderBy(t => t.ExtensionDeadline ?? t.PublishedDeadline)
            .ToListAsync();
    }

    public async Task<IReadOnlyList<TaskEntity>> GetDueBetweenAsync(DateTime fromUtc, DateTime toUtc)
    {
        return await _context.Tasks.AsNoTracking()
            .Where(t => (t.Status == TaskStatus.Assigned || t.Status == TaskStatus.RevisionRequested)
                        && (t.ExtensionDeadline ?? t.PublishedDeadline) >= fromUtc
                        && (t.ExtensionDeadline ?? t.PublishedDeadline) < toUtc)
            .OrderBy(t => t.ExtensionDeadline ?? t.PublishedDeadline)
            .ToListAsync();
    }

    public async Task<bool> TryChangeStatusAsync(int taskId, TaskStatus expectedStatus, TaskStatus newStatus, DateTime utcNow)
    {
        var affected = await _context.Tasks
            .Where(t => t.Id == taskId && t.Status == expectedStatus)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, newStatus)
                .SetProperty(t => t.UpdatedAt, (DateTime?)utcNow));

        return affected == 1;
    }

    public async Task<bool> TryAssignAsync(int taskId, int freelancerProfileId, DateTime utcNow)
    {
        var affected = await _context.Tasks
            .Where(t => t.Id == taskId
                        && t.Status == TaskStatus.Published
                        && t.AssignedFreelancerId == null)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.AssignedFreelancerId, (int?)freelancerProfileId)
                .SetProperty(t => t.Status, TaskStatus.Assigned)
                .SetProperty(t => t.UpdatedAt, (DateTime?)utcNow));

        return affected == 1;
    }

    public async Task<bool> TryReassignAsync(
        int taskId,
        int previousFreelancerProfileId,
        int? newFreelancerProfileId,
        TaskStatus resultingStatus,
        DateTime utcNow)
    {
        var affected = await _context.Tasks
            .Where(t => t.Id == taskId
                        && t.Status == TaskStatus.Assigned
                        && t.AssignedFreelancerId == previousFreelancerProfileId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.AssignedFreelancerId, newFreelancerProfileId)
                .SetProperty(t => t.Status, resultingStatus)
                .SetProperty(t => t.UpdatedAt, (DateTime?)utcNow));

        return affected == 1;
    }
}
