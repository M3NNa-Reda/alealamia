using alealamia.Application.DTOs.Files;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using FileEntity = alealamia.Domain.Entities.File;

namespace alealamia.Application.Services
{
    public class FileService : IFileService
    {
        private const long MaxFileSize = 100 * 1024 * 1024; // 100 MB

        private readonly IUnitOfWork _unitOfWork;
        private readonly IFileStorage _fileStorage;

        public FileService(IUnitOfWork unitOfWork, IFileStorage fileStorage)
        {
            _unitOfWork = unitOfWork;
            _fileStorage = fileStorage;
        }

        public async Task<FileDto> UploadFileAsync(UploadFileDto dto, string uploadedByUserId)
        {
            if (dto == null || dto.File == null || dto.File.Length == 0)
                throw new ArgumentException("File is required.");

            if (string.IsNullOrWhiteSpace(uploadedByUserId))
                throw new ArgumentException("User id is required.");

            if (dto.File.Length > MaxFileSize)
                throw new ArgumentException("File size cannot be more than 100 MB.");

            var originalName = Path.GetFileName(dto.File.FileName);
            var extension = Path.GetExtension(originalName);
            var storedName = Guid.NewGuid() + extension;

            string fileUrl;
            using (var stream = dto.File.OpenReadStream())
            {
                fileUrl = await _fileStorage.SaveAsync(stream, storedName);
            }

            var file = new FileEntity
            {
                OriginalFileName = originalName,
                StoredFileName = storedName,
                FileUrl = fileUrl,
                ContentType = string.IsNullOrWhiteSpace(dto.File.ContentType)
                    ? "application/octet-stream"
                    : dto.File.ContentType,
                SizeInBytes = dto.File.Length,
                UploadedByUserId = uploadedByUserId,
                UploadedAt = DateTime.UtcNow
            };

            try
            {
                await _unitOfWork.FileRepository.AddAsync(file);
                await _unitOfWork.CompleteAsync();
            }
            catch
            {
                await _fileStorage.DeleteAsync(storedName);
                throw;
            }

            return new FileDto
            {
                Id = file.Id,
                OriginalFileName = file.OriginalFileName,
                FileUrl = file.FileUrl,
                ContentType = file.ContentType,
                SizeInBytes = file.SizeInBytes,
                UploadedAt = file.UploadedAt
            };
        }

        public async Task<FileDetailsDto?> GetByIdAsync(int id)
        {
            var file = await _unitOfWork.FileRepository.GetByIdAsync(id);

            if (file == null)
                return null;

            return new FileDetailsDto
            {
                Id = file.Id,
                OriginalFileName = file.OriginalFileName,
                FileUrl = file.FileUrl,
                ContentType = file.ContentType,
                SizeInBytes = file.SizeInBytes,
                UploadedAt = file.UploadedAt,
                StoredFileName = file.StoredFileName,
                UploadedByUserId = file.UploadedByUserId
            };
        }
    }
}