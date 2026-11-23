using alealamia.Application.DTOs.Admin;
using alealamia.Application.DTOs.Common;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;


namespace alealamia.Application.Services
{
    public class AdminFreelancerService : IAdminFreelancerService
    {
        private readonly IUnitOfWork _unitOfWork;

        public AdminFreelancerService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<ResultDto> ApproveFreelancerAsync(int freelancerId)
        {
            throw new NotImplementedException();

        }
        public async Task<ResultDto> RejectFreelancerAsync(int freelancerId)
        {
            throw new NotImplementedException();
        }

        public async Task<FreelancerReviewDetailsDto?> GetFreelancerDetailsAsync(int id)
        {
            throw new NotImplementedException();

        }

        public Task<IEnumerable<FreelancerReviewListDto>> GetPendingFreelancersAsync()
        {
            throw new NotImplementedException();
        }

        private static ResultDto Failed(string error)
        {
            return new ResultDto
            {
                Succeeded = false,
                Errors = new List<string> { error }
            };
        }
    }
}
