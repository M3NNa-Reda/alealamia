namespace alealamia.Application.DTOs.Files
{
    public class FileDetailsDto : FileDto
    {
        public string StoredFileName { get; set; } = string.Empty;
        public string UploadedByUserId { get; set; } = string.Empty; 
    }
}
