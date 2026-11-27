using alealamia.Application.Files;

namespace alealamia.Application.Tests;

public class FileRulesTests
{
    private static byte[] Bytes(params byte[] bytes) => bytes;

    [Theory]
    [InlineData(".pdf", new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D })]
    [InlineData(".png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 })]
    [InlineData(".jpg", new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 })]
    [InlineData(".JPEG", new byte[] { 0xFF, 0xD8, 0xFF, 0xE1 })]
    [InlineData(".docx", new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x14 })]
    [InlineData(".zip", new byte[] { 0x50, 0x4B, 0x03, 0x04, 0x0A })]
    [InlineData(".txt", new byte[] { 0x68, 0x65, 0x6C, 0x6C, 0x6F })]
    public void ContentMatches_AcceptsTheRealSignature(string extension, byte[] header)
    {
        Assert.True(FileRules.IsAllowedExtension(extension));
        Assert.True(FileRules.ContentMatches(extension, header, header.Length));
    }

    [Fact]
    public void ContentMatches_AcceptsAWebp()
    {
        var header = Bytes(0x52, 0x49, 0x46, 0x46, 0x10, 0, 0, 0, 0x57, 0x45, 0x42, 0x50);
        Assert.True(FileRules.ContentMatches(".webp", header, header.Length));
    }

    [Theory]
    [InlineData(".png")]
    [InlineData(".pdf")]
    [InlineData(".docx")]
    [InlineData(".jpg")]
    public void ContentMatches_RejectsAProgramRenamedToADocumentOrPicture(string extension)
    {
        var windowsExecutable = Bytes(0x4D, 0x5A, 0x90, 0x00, 0x03);
        Assert.False(FileRules.ContentMatches(extension, windowsExecutable, windowsExecutable.Length));
    }

    [Fact]
    public void ContentMatches_RejectsATextFileWithBinaryContent()
    {
        var header = Bytes(0x68, 0x00, 0x69);
        Assert.False(FileRules.ContentMatches(".txt", header, header.Length));
    }

    [Fact]
    public void ContentMatches_RejectsAnEmptyHeader()
        => Assert.False(FileRules.ContentMatches(".pdf", new byte[8], 0));

    [Theory]
    [InlineData(".exe")]
    [InlineData(".html")]
    [InlineData(".svg")]
    [InlineData(".js")]
    [InlineData(".php")]
    [InlineData(".config")]
    [InlineData("")]
    [InlineData(null)]
    public void DangerousOrUnknownExtensions_AreNotAllowed(string? extension)
    {
        Assert.False(FileRules.IsAllowedExtension(extension));
        Assert.Null(FileRules.ContentTypeFor(extension));
    }

    [Theory]
    [InlineData(".pdf", "application/pdf")]
    [InlineData(".PNG", "image/png")]
    [InlineData(".jpeg", "image/jpeg")]
    public void ContentType_IsDerivedFromTheExtension(string extension, string expected)
        => Assert.Equal(expected, FileRules.ContentTypeFor(extension));

    [Theory]
    [InlineData("report.pdf", "report.pdf")]
    [InlineData(@"..\..\windows\evil.png", "evil.png")]
    [InlineData("../../etc/passwd.txt", "passwd.txt")]
    [InlineData("a\r\nb.pdf", "ab.pdf")]
    [InlineData("na\"me<>.pdf", "name.pdf")]
    [InlineData("", "file")]
    [InlineData("....", "file")]
    [InlineData(null, "file")]
    public void SanitizeFileName_KeepsOnlyASafeName(string? input, string expected)
        => Assert.Equal(expected, FileRules.SanitizeFileName(input));

    [Fact]
    public void SanitizeFileName_ShortensAVeryLongNameButKeepsTheExtension()
    {
        var name = new string('a', 500) + ".pdf";

        var cleaned = FileRules.SanitizeFileName(name);

        Assert.Equal(FileRules.MaxFileNameLength, cleaned.Length);
        Assert.EndsWith(".pdf", cleaned);
    }
}
