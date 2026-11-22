using alealamia.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Application.Interfaces.Repositories
{
    public interface IFreelancerBlockRepository : IGenericRepository<FreelancerBlock>
    {
        Task<IEnumerable<FreelancerBlock>> GetActiveBlocksByFreelancerIdAsync(int freelancerProfileId, DateTime currentDateTime);
        Task<IEnumerable<FreelancerBlock>>GetBlockHistoryByFreelancerIdAsync(int freelancerProfileId);
        Task<IEnumerable<FreelancerBlock>>GetAllActiveBlocksAsync(DateTime currentDateTime);
    }
}
