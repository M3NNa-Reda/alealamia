using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface IFreelancerProfileRepository:IGenericRepository<FreelancerProfile>
    {
        Task<FreelancerProfile?> GetByUserIdAsync(string userId);
        Task<IEnumerable<FreelancerProfile>> GetFreelancersByApprovalStatusAsync(FreelancerApprovalStatus status);
        Task<IEnumerable<FreelancerProfile>> GetActiveFreelancersAsync();

    }
}
