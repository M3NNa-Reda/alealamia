using alealamia.Domain.Enums;

namespace alealamia.Application.DTOs.MarketingPackages
{
    public class MarketingPackageDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMonths { get; set; }
        public decimal DefaultPrice { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<MarketingPackageItemDto> Items { get; set; } = new();
    }

    public class CreateMarketingPackageDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMonths { get; set; }
        public decimal DefaultPrice { get; set; }
    }

    public class UpdateMarketingPackageDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DurationMonths { get; set; }
        public decimal DefaultPrice { get; set; }
        public bool IsActive { get; set; }
    }

    public class MarketingPackageItemDto
    {
        public int Id { get; set; }
        public int MarketingPackageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DefaultQuantity { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateMarketingPackageItemDto
    {
        public int MarketingPackageId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DefaultQuantity { get; set; }
    }

    public class UpdateMarketingPackageItemDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DefaultQuantity { get; set; }
        public bool IsActive { get; set; }
    }

    public class ProjectMarketingPackageDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public int MarketingPackageId { get; set; }
        public string PackageName { get; set; } = string.Empty;
        public decimal PriceAtPurchase { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public MarketingSubscriptionStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ProjectMarketingPackageItemDto> Items { get; set; } = new();
    }

    public class PurchaseMarketingPackageDto
    {
        public int ProjectId { get; set; }
        public int MarketingPackageId { get; set; }
        public DateTime StartDate { get; set; }
    }

    public class ProjectMarketingPackageItemDto
    {
        public int Id { get; set; }
        public int ProjectMarketingPackageId { get; set; }
        public int? OriginalPackageItemId { get; set; }
        public string NameSnapshot { get; set; } = string.Empty;
        public string? DescriptionSnapshot { get; set; }
        public int Quantity { get; set; }
    }
}