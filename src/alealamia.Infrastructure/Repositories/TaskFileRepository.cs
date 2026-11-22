using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace alealamia.Infrastructure.Repositories;

public class TaskFileRepository : ITaskFileRepository
{
    private readonly ApplicationDbContext _context;

    public TaskFileRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TaskFile>> GetByTaskIdAsync(int taskId)
    {
        return await _context.TaskFiles.AsNoTracking()
            .Include(f => f.File)
            .Where(f => f.TaskId == taskId)
            .OrderBy(f => f.AddedAt)
            .ToListAsync();
    }

    public async Task<TaskFile?> GetAsync(int taskId, int fileId)
    {
        return await _context.TaskFiles
            .FirstOrDefaultAsync(f => f.TaskId == taskId && f.FileId == fileId);
    }

    public async Task AddAsync(TaskFile taskFile)
    {
        await _context.TaskFiles.AddAsync(taskFile);
    }

    public void Delete(TaskFile taskFile)
    {
        _context.TaskFiles.Remove(taskFile);
    }
}
