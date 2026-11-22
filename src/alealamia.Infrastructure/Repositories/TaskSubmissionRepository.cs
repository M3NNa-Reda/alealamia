using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;


namespace alealamia.Infrastructure.Repositories
{
    public class TaskSubmissionRepository : GenericRepository<TaskSubmission>, ITaskSubmissionRepository
    {
        public TaskSubmissionRepository(ApplicationDbContext context) : base(context)
        {
        }
        public async Task<IEnumerable<TaskSubmission>> GetByTaskIdAsync(int id)
        {
            return await _context.TaskSubmissions
                    .Where(t => t.TaskId == id)
                    .AsNoTracking()
                    .ToListAsync();
        }
        public async Task<IEnumerable<TaskSubmission>> GetByFreelancerProfileIdAsync(int id)
        {
            return await _context.TaskSubmissions
                   .Where(t => t.FreelancerProfileId == id)
                   .AsNoTracking()
                   .ToListAsync();
        }
        public async Task<IEnumerable<TaskSubmission>> GetByStatusAsync(SubmissionStatus status)
        {
            return await _context.TaskSubmissions
                    .AsNoTracking()
                    .Where(t=>t.Status==status)
                    .ToListAsync();
        }
        public async Task<IEnumerable<TaskSubmission>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.TaskSubmissions
                    .AsNoTracking()
                    .Where(t => t.SubmittedAt >= fromDate && t.SubmittedAt <= toDate)
                    .ToListAsync();
        }

    }
}
