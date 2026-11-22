using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class TaskReviewRepository : GenericRepository<TaskReview>, ITaskReviewRepository
    {
        public TaskReviewRepository(ApplicationDbContext context) : base(context)
        {
        }
        public async Task<IEnumerable<TaskReview>> GetByTaskSubmissionIdAsync(int id)
        {
            return await _context.TaskReviews
                    .AsNoTracking()
                    .Where(t => t.TaskSubmissionId == id)
                    .ToListAsync();
        }
        public async Task<IEnumerable<TaskReview>> GetByReviewedByUserIdAsync(string id)
        {
            return await _context.TaskReviews
                    .AsNoTracking()
                    .Where(t => t.ReviewedByUserId == id)
                    .ToListAsync();
        }
        public async Task<IEnumerable<TaskReview>> GetByDecisionAsync(ReviewDecision decision)
        {
            return await _context.TaskReviews
                    .AsNoTracking()
                    .Where(t => t.Decision == decision)
                    .ToListAsync();
        }
        public async Task<IEnumerable<TaskReview>> GetByDateRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.TaskReviews
                    .AsNoTracking()
                    .Where(t => t.ReviewedAt >= fromDate && t.ReviewedAt <= toDate)
                    .ToListAsync();
        }
    }
}
