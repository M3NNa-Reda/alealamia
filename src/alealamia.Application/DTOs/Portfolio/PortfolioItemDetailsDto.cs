namespace alealamia.Application.DTOs.Portfolio
{
    public class PortfolioItemDetailsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ProjectUrl { get; set; }
        public bool IsPublished { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<PortfolioItemFileDto> Files { get; set; } = new();
    }
}
