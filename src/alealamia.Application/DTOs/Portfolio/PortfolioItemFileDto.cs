namespace alealamia.Application.DTOs.Portfolio
{
    public class PortfolioItemFileDto
    {
        public int FileId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public int DisplayOrder { get; set; }
    }
}
