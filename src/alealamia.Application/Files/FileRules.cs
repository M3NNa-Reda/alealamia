namespace alealamia.Application.Files
{
    /// <summary>
    /// What may be uploaded: a short allowlist of extensions, a size limit, and a check that the first bytes of the
    /// file really are what the extension claims. The content type is derived here and never taken from the client.
    /// </summary>
    public static class FileRules
    {
        public const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10 MB (design document, section 5.4)
        public const int MaxFileNameLength = 200;

        /// <summary>How many bytes from the start of the file are needed to validate it.</summary>
        public const int HeaderLength = 8192;

        private sealed record FileKind(string ContentType, Func<byte[], int, bool> Matches);

        private static readonly Dictionary<string, FileKind> Allowed = new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = new("application/pdf", (h, n) => StartsWith(h, n, 0x25, 0x50, 0x44, 0x46)),               // %PDF
            [".png"] = new("image/png", (h, n) => StartsWith(h, n, 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A)),
            [".jpg"] = new("image/jpeg", (h, n) => StartsWith(h, n, 0xFF, 0xD8, 0xFF)),
            [".jpeg"] = new("image/jpeg", (h, n) => StartsWith(h, n, 0xFF, 0xD8, 0xFF)),
            [".webp"] = new("image/webp", IsWebp),
            [".zip"] = new("application/zip", IsZip),
            [".docx"] = new("application/vnd.openxmlformats-officedocument.wordprocessingml.document", IsZip),
            [".xlsx"] = new("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", IsZip),
            [".pptx"] = new("application/vnd.openxmlformats-officedocument.presentationml.presentation", IsZip),
            [".txt"] = new("text/plain", IsText)
        };

        public static IReadOnlyCollection<string> AllowedExtensions => Allowed.Keys;

        public static bool IsAllowedExtension(string? extension)
            => !string.IsNullOrEmpty(extension) && Allowed.ContainsKey(extension);

        /// <summary>The content type stored for this extension, or null when the extension is not allowed.</summary>
        public static string? ContentTypeFor(string? extension)
            => !string.IsNullOrEmpty(extension) && Allowed.TryGetValue(extension, out var kind) ? kind.ContentType : null;

        /// <summary>True when the leading bytes match what the extension claims.</summary>
        public static bool ContentMatches(string extension, byte[] header, int length)
            => Allowed.TryGetValue(extension, out var kind) && length > 0 && kind.Matches(header, length);

        /// <summary>Keeps only the file name, drops control and path characters, and limits the length.</summary>
        public static string SanitizeFileName(string? fileName)
        {
            var name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/'));
            var cleaned = new string(name.Where(c => !char.IsControl(c) && c != '"' && c != '<' && c != '>' && c != '|' && c != ':' && c != '*' && c != '?').ToArray()).Trim().Trim('.');

            if (cleaned.Length == 0)
                return "file";

            if (cleaned.Length <= MaxFileNameLength)
                return cleaned;

            var extension = Path.GetExtension(cleaned);
            var stem = Path.GetFileNameWithoutExtension(cleaned);
            return stem[..Math.Max(1, MaxFileNameLength - extension.Length)] + extension;
        }

        private static bool StartsWith(byte[] header, int length, params byte[] signature)
        {
            if (length < signature.Length)
                return false;

            for (var i = 0; i < signature.Length; i++)
            {
                if (header[i] != signature[i])
                    return false;
            }

            return true;
        }

        // A zip archive (and the Office formats built on it) starts with "PK" and a local file header.
        private static bool IsZip(byte[] header, int length)
            => StartsWith(header, length, 0x50, 0x4B, 0x03, 0x04);

        // "RIFF" ???? "WEBP"
        private static bool IsWebp(byte[] header, int length)
            => length >= 12
               && StartsWith(header, length, 0x52, 0x49, 0x46, 0x46)
               && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;

        // Plain text never contains NUL bytes; executables and most binary formats do.
        private static bool IsText(byte[] header, int length)
        {
            for (var i = 0; i < length; i++)
            {
                if (header[i] == 0)
                    return false;
            }

            return true;
        }
    }
}
