using alealamia.Application.DTOs.Files;

namespace alealamia.Application.Interfaces.Services
{
    public interface IFileService
    {
        Task<FileDto> UploadFileAsync(UploadFileDto dto, string uploadedByUserId);

        Task<FileDetailsDto?> GetByIdAsync(int id);
    }
}