namespace alealamia.Application.DTOs.Files
{
    public class FileDto
    {
        public int Id { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long SizeInBytes { get; set; }
        public DateTime UploadedAt { get; set; } 
    }
}
