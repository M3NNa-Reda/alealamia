using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;

namespace alealamia.Infrastructure.Repositories
{
    public class MeetingRepository : GenericRepository<Meeting>, IMeetingRepository
    {
        public MeetingRepository(ApplicationDbContext context) : base(context) { }

        public async Task<IEnumerable<Meeting>> GetByProjectIdAsync(int projectId)
        {
            return await _context.Meetings.AsNoTracking()
                .Where(m => m.ProjectId == projectId)
                .OrderByDescending(m => m.ProposedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Meeting>> GetByClientProfileIdAsync(int clientProfileId)
        {
            return await _context.Meetings.AsNoTracking()
                .Where(m => m.ClientProfileId == clientProfileId)
                .OrderByDescending(m => m.ProposedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Meeting>> GetByStatusAsync(MeetingStatus status)
        {
            return await _context.Meetings.AsNoTracking()
                .Where(m => m.Status == status)
                .ToListAsync();
        }
    }
}