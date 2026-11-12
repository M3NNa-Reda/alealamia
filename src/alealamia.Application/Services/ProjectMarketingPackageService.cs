using alealamia.Application.DTOs.MarketingPackages;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using Task = System.Threading.Tasks.Task;

namespace alealamia.Application.Services
{
    public class ProjectMarketingPackageService : IProjectMarketingPackageService
    {
        private readonly IProjectMarketingPackageRepository _purchaseRepository;
        private readonly IMarketingPackageRepository _packageRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IUnitOfWork _unitOfWork;

        public ProjectMarketingPackageService(
            IProjectMarketingPackageRepository purchaseRepository,
            IMarketingPackageRepository packageRepository,
            IProjectRepository projectRepository,
            IUnitOfWork unitOfWork)
        {
            _purchaseRepository = purchaseRepository;
            _packageRepository = packageRepository;
            _projectRepository = projectRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<ProjectMarketingPackageDto> PurchaseAsync(PurchaseMarketingPackageDto dto)
        {
            var project = await _projectRepository.GetByIdAsync(dto.ProjectId)
                ?? throw new KeyNotFoundException($"Project {dto.ProjectId} was not found.");

            if (project.Status == ProjectStatus.Cancelled)
                throw new InvalidOperationException("Packages cannot be purchased for a cancelled project.");

            var package = await _packageRepository.GetWithItemsAsync(dto.MarketingPackageId)
                ?? throw new KeyNotFoundException($"Marketing package {dto.MarketingPackageId} was not found.");

            if (!package.IsActive)
                throw new InvalidOperationException("This package is not available.");

            var startDate = dto.StartDate == default ? DateTime.UtcNow : dto.StartDate;

            var existing = await _purchaseRepository.GetByProjectIdAsync(dto.ProjectId);
            if (existing.Any(p => p.MarketingPackageId == package.Id
                                  && p.Status == MarketingSubscriptionStatus.Active
                                  && p.EndDate > DateTime.UtcNow))
                throw new InvalidOperationException("This project already has an active subscription to this package.");

            var purchase = new ProjectMarketingPackage
            {
                ProjectId = dto.ProjectId,
                MarketingPackageId = package.Id,
                PriceAtPurchase = package.DefaultPrice,
                StartDate = startDate,
                EndDate = startDate.AddMonths(package.DurationMonths),
                Status = MarketingSubscriptionStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            foreach (var item in package.Items.Where(i => i.IsActive))
            {
                purchase.Items.Add(new ProjectMarketingPackageItem
                {
                    OriginalPackageItemId = item.Id,
                    NameSnapshot = item.Name,
                    DescriptionSnapshot = item.Description,
                    Quantity = item.DefaultQuantity
                });
            }

            await _purchaseRepository.AddAsync(purchase);
            await _unitOfWork.CompleteAsync();
            return MapToDto(purchase, package.Name);
        }

        public async Task<ProjectMarketingPackageDto?> GetByIdAsync(int id)
        {
            var purchase = await _purchaseRepository.GetWithItemsAsync(id);
            if (purchase is null)
                return null;

            var package = await _packageRepository.GetByIdAsync(purchase.MarketingPackageId);
            return MapToDto(purchase, package?.Name ?? string.Empty);
        }

        public async Task<IEnumerable<ProjectMarketingPackageDto>> GetByProjectAsync(int projectId)
        {
            var purchases = await _purchaseRepository.GetByProjectIdAsync(projectId);
            return purchases.Select(p => MapToDto(p, p.MarketingPackage?.Name ?? string.Empty));
        }

        public async Task CancelAsync(int id)
        {
            var purchase = await _purchaseRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Subscription {id} was not found.");

            if (purchase.Status != MarketingSubscriptionStatus.Active)
                throw new InvalidOperationException("Only active subscriptions can be cancelled.");

            purchase.Status = MarketingSubscriptionStatus.Cancelled;

            _purchaseRepository.Update(purchase);
            await _unitOfWork.CompleteAsync();
        }

        private static ProjectMarketingPackageDto MapToDto(ProjectMarketingPackage p, string packageName) => new()
        {
            Id = p.Id,
            ProjectId = p.ProjectId,
            MarketingPackageId = p.MarketingPackageId,
            PackageName = packageName,
            PriceAtPurchase = p.PriceAtPurchase,
            StartDate = p.StartDate,
            EndDate = p.EndDate,
            Status = p.Status,
            CreatedAt = p.CreatedAt,
            Items = p.Items.Select(i => new ProjectMarketingPackageItemDto
            {
                Id = i.Id,
                ProjectMarketingPackageId = i.ProjectMarketingPackageId,
                OriginalPackageItemId = i.OriginalPackageItemId,
                NameSnapshot = i.NameSnapshot,
                DescriptionSnapshot = i.DescriptionSnapshot,
                Quantity = i.Quantity
            }).ToList()
        };
    }
}