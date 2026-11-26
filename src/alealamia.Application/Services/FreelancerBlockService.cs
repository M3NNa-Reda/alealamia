using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.FreelancerBlock;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;

namespace alealamia.Application.Services
{
    public class FreelancerBlockService : IFreelancerBlockService
    {
        private readonly IFreelancerBlockRepository _blockRepository;
        private readonly IUnitOfWork _unitOfWork;

        public FreelancerBlockService(
            IFreelancerBlockRepository blockRepository,
            IUnitOfWork unitOfWork)
        {
            _blockRepository = blockRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ResultDto> CreateBlockAsync(CreateFreelancerBlockDto dto)
        {
            if (dto.FreelancerProfileId <= 0)
                return Failed("Invalid freelancer profile ID.");

            if (string.IsNullOrWhiteSpace(dto.Reason))
                return Failed("Block reason is required.");

            if (string.IsNullOrWhiteSpace(dto.CreatedByUserId))
                return Failed("CreatedByUserId is required.");

            if (dto.EndsAt <= dto.StartedAt)
                return Failed("Block end date must be after start date.");

            var block = new FreelancerBlock
            {
                FreelancerProfileId = dto.FreelancerProfileId,
                TaskId = dto.TaskId,
                Reason = dto.Reason.Trim(),
                StartedAt = dto.StartedAt,
                EndsAt = dto.EndsAt,
                CreatedByUserId = dto.CreatedByUserId
            };

            await _blockRepository.AddAsync(block);
            await _unitOfWork.CompleteAsync();

            return new ResultDto
            {
                Succeeded = true
            };
        }

        public async Task<IEnumerable<FreelancerBlockDto>>
            GetActiveBlocksByFreelancerIdAsync(int freelancerProfileId)
        {
            var blocks = await _blockRepository
                .GetActiveBlocksByFreelancerIdAsync(
                    freelancerProfileId, DateTime.UtcNow);

            return blocks.Select(MapToDto);
        }

        public async Task<IEnumerable<FreelancerBlockDto>>
            GetAllActiveBlocksAsync()
        {
            var blocks = await _blockRepository
                .GetAllActiveBlocksAsync(DateTime.UtcNow);

            return blocks.Select(MapToDto);
        }

        public async Task<IEnumerable<FreelancerBlockDto>>
            GetBlockHistoryByFreelancerIdAsync(int freelancerProfileId)
        {
            var blocks = await _blockRepository
                .GetBlockHistoryByFreelancerIdAsync(freelancerProfileId);

            return blocks.Select(MapToDto);
        }

        private static FreelancerBlockDto MapToDto(FreelancerBlock block)
        {
            return new FreelancerBlockDto
            {
                Id = block.Id,
                FreelancerProfileId = block.FreelancerProfileId,
                TaskId = block.TaskId,
                Reason = block.Reason,
                StartedAt = block.StartedAt,
                EndsAt = block.EndsAt
            };
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