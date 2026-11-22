using alealamia.Application.Interfaces.Repositories;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Infrastructure.Repositories
{
    public class FreelancerProfileRepository : GenericRepository<FreelancerProfile>, IFreelancerProfileRepository
    {
        public FreelancerProfileRepository(ApplicationDbContext context) : base(context)
        {
        }

        public async Task<IEnumerable<FreelancerProfile>> GetActiveFreelancersAsync()
        {
            return await _context.FreelancerProfiles
                .Where(x => x.IsAvailable)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<IEnumerable<FreelancerProfile>> GetFreelancersByApprovalStatusAsync(FreelancerApprovalStatus status)
        {
            return await _context.FreelancerProfiles
                .Where(x => x.ApprovalStatus == status)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<FreelancerProfile?> GetByUserIdAsync(string userId)
        {
            return await _context.FreelancerProfiles
                .FirstOrDefaultAsync(x => x.UserId == userId);
        }
    }
}
