using alealamia.Application.DTOs.Files;

namespace alealamia.Application.Interfaces.Services
{
    public interface IFileService
    {
        Task<FileDto> UploadFileAsync(UploadFileDto dto, string uploadedByUserId);

        Task<FileDetailsDto?> GetByIdAsync(int id);

        /// <summary>Opens the file for download. Returns null when the record or the stored file is missing.</summary>
        Task<FileContentDto?> OpenAsync(int id);
    }
}
