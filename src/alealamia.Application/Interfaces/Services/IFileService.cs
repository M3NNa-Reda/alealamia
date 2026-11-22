
namespace alealamia.Application.Interfaces.Services
{
    public interface IFileService
    {
        Task<Domain.Entities.File> UploadFileAsync(Stream content, string originalFileName, string contentType, long sizeInBytes, string uploadedByUserId);

        Task<Domain.Entities.File?> GetByIdAsync(int id);

        Task<bool> DeleteFileAsync(int id);
    }
}