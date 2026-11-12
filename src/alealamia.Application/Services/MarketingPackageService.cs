using alealamia.Application.DTOs.MarketingPackages;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;
using Task = System.Threading.Tasks.Task;

namespace alealamia.Application.Services
{
    public class MarketingPackageService : IMarketingPackageService
    {
        private readonly IMarketingPackageRepository _packageRepository;
        private readonly IMarketingPackageItemRepository _itemRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MarketingPackageService(
            IMarketingPackageRepository packageRepository,
            IMarketingPackageItemRepository itemRepository,
            IUnitOfWork unitOfWork)
        {
            _packageRepository = packageRepository;
            _itemRepository = itemRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<MarketingPackageDto>> GetAllAsync(bool activeOnly = false)
        {
            var packages = activeOnly
                ? await _packageRepository.GetActivePackagesAsync()
                : await _packageRepository.GetAllAsync();

            return packages.Select(MapToDto);
        }

        public async Task<MarketingPackageDto?> GetByIdAsync(int id)
        {
            var package = await _packageRepository.GetWithItemsAsync(id);
            return package is null ? null : MapToDto(package);
        }

        public async Task<MarketingPackageDto> CreateAsync(CreateMarketingPackageDto dto)
        {
            ValidatePackage(dto.Name, dto.DurationMonths, dto.DefaultPrice);

            var package = new MarketingPackage
            {
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                DurationMonths = dto.DurationMonths,
                DefaultPrice = dto.DefaultPrice,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            await _packageRepository.AddAsync(package);
            await _unitOfWork.CompleteAsync();
            return MapToDto(package);
        }

        public async Task<MarketingPackageDto> UpdateAsync(int id, UpdateMarketingPackageDto dto)
        {
            ValidatePackage(dto.Name, dto.DurationMonths, dto.DefaultPrice);

            var package = await _packageRepository.GetWithItemsAsync(id)
                ?? throw new KeyNotFoundException($"Marketing package {id} was not found.");

            package.Name = dto.Name.Trim();
            package.Description = dto.Description?.Trim();
            package.DurationMonths = dto.DurationMonths;
            package.DefaultPrice = dto.DefaultPrice;
            package.IsActive = dto.IsActive;
            package.UpdatedAt = DateTime.UtcNow;

            _packageRepository.Update(package);
            await _unitOfWork.CompleteAsync();
            return MapToDto(package);
        }

        public async Task DeactivateAsync(int id)
        {
            var package = await _packageRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Marketing package {id} was not found.");

            package.IsActive = false;
            package.UpdatedAt = DateTime.UtcNow;

            _packageRepository.Update(package);
            await _unitOfWork.CompleteAsync();
        }

        public async Task<MarketingPackageItemDto> AddItemAsync(CreateMarketingPackageItemDto dto)
        {
            var package = await _packageRepository.GetByIdAsync(dto.MarketingPackageId)
                ?? throw new KeyNotFoundException($"Marketing package {dto.MarketingPackageId} was not found.");

            ValidateItem(dto.Name, dto.DefaultQuantity);

            var item = new MarketingPackageItem
            {
                MarketingPackageId = package.Id,
                Name = dto.Name.Trim(),
                Description = dto.Description?.Trim(),
                DefaultQuantity = dto.DefaultQuantity,
                IsActive = true
            };

            await _itemRepository.AddAsync(item);
            await _unitOfWork.CompleteAsync();
            return MapItemToDto(item);
        }

        public async Task<MarketingPackageItemDto> UpdateItemAsync(int itemId, UpdateMarketingPackageItemDto dto)
        {
            var item = await _itemRepository.GetByIdAsync(itemId)
                ?? throw new KeyNotFoundException($"Marketing package item {itemId} was not found.");

            ValidateItem(dto.Name, dto.DefaultQuantity);

            item.Name = dto.Name.Trim();
            item.Description = dto.Description?.Trim();
            item.DefaultQuantity = dto.DefaultQuantity;
            item.IsActive = dto.IsActive;

            _itemRepository.Update(item);
            await _unitOfWork.CompleteAsync();
            return MapItemToDto(item);
        }

        public async Task DeactivateItemAsync(int itemId)
        {
            var item = await _itemRepository.GetByIdAsync(itemId)
                ?? throw new KeyNotFoundException($"Marketing package item {itemId} was not found.");

            item.IsActive = false;

            _itemRepository.Update(item);
            await _unitOfWork.CompleteAsync();
        }

        private static void ValidatePackage(string name, int durationMonths, decimal defaultPrice)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Package name is required.");
            if (durationMonths <= 0)
                throw new ArgumentException("Duration must be at least one month.");
            if (defaultPrice < 0)
                throw new ArgumentException("Price cannot be negative.");
        }

        private static void ValidateItem(string name, int quantity)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Item name is required.");
            if (quantity <= 0)
                throw new ArgumentException("Quantity must be greater than zero.");
        }

        private static MarketingPackageItemDto MapItemToDto(MarketingPackageItem i) => new()
        {
            Id = i.Id,
            MarketingPackageId = i.MarketingPackageId,
            Name = i.Name,
            Description = i.Description,
            DefaultQuantity = i.DefaultQuantity,
            IsActive = i.IsActive
        };

        private static MarketingPackageDto MapToDto(MarketingPackage p) => new()
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            DurationMonths = p.DurationMonths,
            DefaultPrice = p.DefaultPrice,
            IsActive = p.IsActive,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Items = p.Items.Select(MapItemToDto).ToList()
        };
    }
}