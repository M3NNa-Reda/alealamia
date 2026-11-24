
namespace alealamia.Application.DTOs.Client
{
    public class ClientProfileDetailsDto
    {
        public int Id { get; set; }
        public string? BusinessName { get; set; }
        public string? BusinessType { get; set; }
        public string? WebsiteUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
