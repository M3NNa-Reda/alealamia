namespace alealamia.Application.DTOs.Files
{
    /// <summary>A stored file opened for download. The caller owns (and disposes) the stream.</summary>
    public sealed class FileContentDto
    {
        public required Stream Content { get; init; }
        public required string ContentType { get; init; }
        public required string FileName { get; init; }
    }
}
