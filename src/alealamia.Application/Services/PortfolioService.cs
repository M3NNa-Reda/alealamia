using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Portfolio;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;
using FileEntity = alealamia.Domain.Entities.File;

namespace alealamia.Application.Services
{
    public class PortfolioService : IPortfolioService
    {
        private readonly IPortfolioRepository _portfolioRepository;
        private readonly IUnitOfWork _unitOfWork;

        public PortfolioService(
            IPortfolioRepository portfolioRepository,
            IUnitOfWork unitOfWork)
        {
            _portfolioRepository = portfolioRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ResultDto> CreatePortfolioItemAsync(
            int freelancerProfileId,
            PortfolioItemDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return Failed("Portfolio title is required.");

            if (string.IsNullOrWhiteSpace(dto.Description))
                return Failed("Portfolio description is required.");

            var item = new PortfolioItem
            {
                FreelancerProfileId = freelancerProfileId,
                Title = dto.Title.Trim(),
                Description = dto.Description.Trim(),
                ProjectUrl = dto.ProjectUrl?.Trim(),
                IsPublished = false,
                CreatedAt = DateTime.UtcNow
            };

            await _portfolioRepository.AddAsync(item);
            await _unitOfWork.CompleteAsync();

            return Success();
        }

        public async Task<ResultDto> UpdatePortfolioItemAsync(
            int freelancerProfileId,
            int portfolioItemId,
            PortfolioItemDto dto)
        {
            var item = await _portfolioRepository
                .GetByIdWithFilesAsync(portfolioItemId);

            if (item == null || item.FreelancerProfileId != freelancerProfileId)
                return Failed("Portfolio item not found.");

            if (string.IsNullOrWhiteSpace(dto.Title))
                return Failed("Portfolio title is required.");

            if (string.IsNullOrWhiteSpace(dto.Description))
                return Failed("Portfolio description is required.");

            item.Title = dto.Title.Trim();
            item.Description = dto.Description.Trim();
            item.ProjectUrl = dto.ProjectUrl?.Trim();
            item.UpdatedAt = DateTime.UtcNow;

            _portfolioRepository.Update(item);
            await _unitOfWork.CompleteAsync();

            return Success();
        }

        public async Task<ResultDto> DeletePortfolioItemAsync(
            int freelancerProfileId,
            int portfolioItemId)
        {
            var item = await _portfolioRepository
                .GetByIdWithFilesAsync(portfolioItemId);

            if (item == null || item.FreelancerProfileId != freelancerProfileId)
                return Failed("Portfolio item not found.");

            var fileLinksRepository =
                _unitOfWork.Repository<PortfolioItemFile>();

            foreach (var fileLink in item.PortfolioItemFiles.ToList())
            {
                fileLinksRepository.Delete(fileLink);
            }

            _portfolioRepository.Delete(item);

            await _unitOfWork.CompleteAsync();

            return Success();
        }

        public async Task<PortfolioItemDetailsDto?> GetPortfolioItemAsync(
            int freelancerProfileId,
            int portfolioItemId)
        {
            var item = await _portfolioRepository
                .GetByIdWithFilesAsync(portfolioItemId);

            if (item == null || item.FreelancerProfileId != freelancerProfileId)
                return null;

            return MapToDto(item);
        }

        public async Task<IEnumerable<PortfolioItemDetailsDto>> GetMyPortfolioAsync(
            int freelancerProfileId)
        {
            var items = await _portfolioRepository
                .GetByFreelancerIdWithFilesAsync(freelancerProfileId);

            return items.Select(MapToDto);
        }

        public async Task<ResultDto> AddFilesAsync(
            int freelancerProfileId,
            int portfolioItemId,
            IEnumerable<int> fileIds)
        {
            var item = await _portfolioRepository
                .GetByIdWithFilesAsync(portfolioItemId);

            if (item == null || item.FreelancerProfileId != freelancerProfileId)
                return Failed("Portfolio item not found.");

            var ids = fileIds.Distinct().ToList();

            if (!ids.Any())
                return Failed("At least one file is required.");

            var freelancer = await _unitOfWork.FreelancerProfiles
                .GetByIdAsync(freelancerProfileId);

            if (freelancer == null)
                return Failed("Freelancer profile not found.");

            var areOwned = await _portfolioRepository
                .AreFilesOwnedByUserAsync(ids, freelancer.UserId);

            if (!areOwned)
                return Failed("One or more files do not belong to this freelancer.");

            var filesRepository = _unitOfWork.Repository<FileEntity>();
            var files = await filesRepository.FindByConditionAsync(
                x => ids.Contains(x.Id));

            if (files.Any(x => !x.ContentType.StartsWith(
                    "image/", StringComparison.OrdinalIgnoreCase)))
            {
                return Failed("Only image files can be added to the portfolio.");
            }

            var existingIds = item.PortfolioItemFiles
                .Select(x => x.FileId)
                .ToHashSet();

            var newIds = ids.Where(id => !existingIds.Contains(id)).ToList();

            if (!newIds.Any())
                return Failed("All selected files are already attached.");
            if (item.PortfolioItemFiles.Count + newIds.Count > 3)
                return Failed("A portfolio item cannot have more than 3 images.");

            var linksRepository =
                _unitOfWork.Repository<PortfolioItemFile>();

            var nextOrder = item.PortfolioItemFiles.Any()
                ? item.PortfolioItemFiles.Max(x => x.DisplayOrder) + 1
                : 1;

            foreach (var fileId in newIds)
            {
                await linksRepository.AddAsync(new PortfolioItemFile
                {
                    PortfolioItemId = portfolioItemId,
                    FileId = fileId,
                    DisplayOrder = nextOrder++
                });
            }

            await _unitOfWork.CompleteAsync();

            return Success();
        }

        public async Task<ResultDto> RemoveFileAsync(
            int freelancerProfileId,
            int portfolioItemId,
            int fileId)
        {
            var item = await _portfolioRepository
                .GetByIdWithFilesAsync(portfolioItemId);

            if (item == null || item.FreelancerProfileId != freelancerProfileId)
                return Failed("Portfolio item not found.");

            var fileLink = item.PortfolioItemFiles
                .FirstOrDefault(x => x.FileId == fileId);

            if (fileLink == null)
                return Failed("File is not attached to this portfolio item.");

            _unitOfWork.Repository<PortfolioItemFile>().Delete(fileLink);

            if (item.IsPublished && item.PortfolioItemFiles.Count - 1 != 3)
            {
                item.IsPublished = false;
                item.UpdatedAt = DateTime.UtcNow;
            }

            await _unitOfWork.CompleteAsync();

            return Success();
        }
        public async Task<ResultDto> PublishPortfolioItemAsync(
            int freelancerProfileId,
            int portfolioItemId)
        {
            var item = await _portfolioRepository
                .GetByIdWithFilesAsync(portfolioItemId);

            if (item == null || item.FreelancerProfileId != freelancerProfileId)
                return Failed("Portfolio item not found.");

            if (item.PortfolioItemFiles.Count != 3)
                return Failed("A portfolio item must have exactly 3 images to be published.");

            item.IsPublished = true;
            item.UpdatedAt = DateTime.UtcNow;

            _portfolioRepository.Update(item);
            await _unitOfWork.CompleteAsync();

            return Success();
        }

        public async Task<ResultDto> UnpublishPortfolioItemAsync(
            int freelancerProfileId,
            int portfolioItemId)
        {
            var item = await _portfolioRepository
                .GetByIdWithFilesAsync(portfolioItemId);

            if (item == null || item.FreelancerProfileId != freelancerProfileId)
                return Failed("Portfolio item not found.");

            item.IsPublished = false;
            item.UpdatedAt = DateTime.UtcNow;

            _portfolioRepository.Update(item);
            await _unitOfWork.CompleteAsync();

            return Success();
        }

        private static PortfolioItemDetailsDto MapToDto(PortfolioItem item)
        {
            return new PortfolioItemDetailsDto
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                ProjectUrl = item.ProjectUrl,
                IsPublished = item.IsPublished,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt,
                Files = item.PortfolioItemFiles
                    .OrderBy(x => x.DisplayOrder)
                    .Select(x => new PortfolioItemFileDto
                    {
                        FileId = x.FileId,
                        OriginalFileName = x.File.OriginalFileName,
                        FileUrl = x.File.FileUrl,
                        DisplayOrder = x.DisplayOrder
                    })
                    .ToList()
            };
        }

        private static ResultDto Success()
        {
            return new ResultDto { Succeeded = true };
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
