using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class SubmissionFileRepository : ISubmissionFileRepository
    {
        private readonly ApplicationDbContext _context;
        public SubmissionFileRepository(ApplicationDbContext context)
        {
            _context = context;
        }
        public async Task<SubmissionFile?> GetByTaskSubmissionAndFileIdAsync(int taskSubmissionId, int fileId)
        {
            return await _context.SubmissionFiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(sf => sf.TaskSubmissionId == taskSubmissionId && sf.FileId == fileId);
                    
        }
        public async Task<IEnumerable<SubmissionFile>> GetByTaskSubmissionIdAsync(int id)
        {
            return await _context.SubmissionFiles
                    .AsNoTracking()
                    .Where(s => s.TaskSubmissionId == id)
                    .ToListAsync();
        }
        public async Task<IEnumerable<SubmissionFile>> GetByFileIdAsync(int id)
        {
            return await _context.SubmissionFiles
                    .AsNoTracking()
                    .Where(f => f.FileId == id)
                    .ToListAsync();
        }
        public async Task<IEnumerable<SubmissionFile>> GetByUploadedAtRangeAsync(DateTime fromDate, DateTime toDate)
        {
            return await _context.SubmissionFiles
                    .AsNoTracking()
                    .Where(t => t.UploadedAt >= fromDate && t.UploadedAt <= toDate)
                    .ToListAsync();
        }
    }
}
