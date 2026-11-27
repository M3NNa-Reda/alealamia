using alealamia.API.Helpers.Extensions;
using alealamia.API.Security;
using alealamia.Application.DTOs.Files;
using alealamia.Application.Files;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace alealamia.API.Controllers;

/// <summary>
/// The single upload endpoint. A file is checked (type, size, content), stored under a generated name and recorded;
/// it is then attached to a task, submission or project through that module's own endpoint.
/// Downloads are never public: they go through the endpoint of the entity the file belongs to.
/// </summary>
[ApiController]
[Route("api/files")]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.Freelancer}")]
public class FilesController : ControllerBase
{
    // The body limit leaves room for the multipart envelope; the exact file limit is enforced by FileService.
    private const long RequestLimit = FileRules.MaxFileSizeBytes + 1024 * 1024;

    private readonly IFileService _fileService;

    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost]
    [EnableRateLimiting(RateLimitingExtensions.UploadPolicy)]
    [RequestSizeLimit(RequestLimit)]
    [RequestFormLimits(MultipartBodyLengthLimit = RequestLimit)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<FileDto>> Upload([FromForm] UploadFileDto dto)
    {
        var file = await _fileService.UploadFileAsync(dto, User.GetUserId());
        return Ok(file);
    }
}
