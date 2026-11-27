using alealamia.Application.DTOs.Files;
using alealamia.Application.Files;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using FileEntity = alealamia.Domain.Entities.File;

namespace alealamia.Application.Services
{
    public class FileService : IFileService
    {
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

            if (dto.File.Length > FileRules.MaxFileSizeBytes)
                throw new ArgumentException(
                    $"File size cannot be more than {FileRules.MaxFileSizeBytes / (1024 * 1024)} MB.");

            var originalName = FileRules.SanitizeFileName(dto.File.FileName);
            var extension = Path.GetExtension(originalName).ToLowerInvariant();

            if (!FileRules.IsAllowedExtension(extension))
                throw new ArgumentException(
                    $"This file type is not allowed. Allowed types: {string.Join(", ", FileRules.AllowedExtensions.Order())}.");

            // The extension must match what is really inside the file (a renamed program is not a picture).
            var header = new byte[FileRules.HeaderLength];
            int read;
            using (var headerStream = dto.File.OpenReadStream())
            {
                read = await headerStream.ReadAtLeastAsync(header, header.Length, throwOnEndOfStream: false);
            }

            if (!FileRules.ContentMatches(extension, header, read))
                throw new ArgumentException("The file content does not match its extension.");

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
                ContentType = FileRules.ContentTypeFor(extension)!,
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

        public async Task<FileContentDto?> OpenAsync(int id)
        {
            var file = await _unitOfWork.FileRepository.GetByIdAsync(id);

            if (file == null)
                return null;

            var content = _fileStorage.OpenRead(file.StoredFileName);

            if (content == null)
                return null;

            return new FileContentDto
            {
                Content = content,
                ContentType = file.ContentType,
                FileName = file.OriginalFileName
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