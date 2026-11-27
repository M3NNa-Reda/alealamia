using alealamia.Application.DTOs.Files;
using alealamia.Application.Files;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Application.Services;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Task = System.Threading.Tasks.Task;
using FileEntity = alealamia.Domain.Entities.File;

namespace alealamia.Application.Tests;

public class FileServiceTests
{
    private static readonly byte[] Pdf = { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x34 };
    private static readonly byte[] Exe = { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00 };

    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();
    private readonly IFileRepository _files = Substitute.For<IFileRepository>();
    private readonly IFileStorage _storage = Substitute.For<IFileStorage>();
    private readonly FileService _service;

    public FileServiceTests()
    {
        _uow.FileRepository.Returns(_files);
        _storage.SaveAsync(Arg.Any<Stream>(), Arg.Any<string>()).Returns(call => "/uploads/" + call.ArgAt<string>(1));
        _service = new FileService(_uow, _storage);
    }

    private static UploadFileDto Upload(string name, byte[] content, string clientContentType = "application/octet-stream", long? length = null)
    {
        var file = Substitute.For<IFormFile>();
        file.FileName.Returns(name);
        file.ContentType.Returns(clientContentType);
        file.Length.Returns(length ?? content.Length);
        file.OpenReadStream().Returns(_ => new MemoryStream(content));
        return new UploadFileDto { File = file };
    }

    [Fact]
    public async Task Upload_StoresAValidFile_WithAServerSideContentType_AndAGeneratedName()
    {
        var result = await _service.UploadFileAsync(Upload("Brief.PDF", Pdf, clientContentType: "text/html"), "user-1");

        Assert.Equal("Brief.PDF", result.OriginalFileName);
        Assert.Equal("application/pdf", result.ContentType);

        await _storage.Received(1).SaveAsync(Arg.Any<Stream>(), Arg.Is<string>(n => n.EndsWith(".pdf") && n != "Brief.PDF" && n.Length == 36 + 4));
        await _files.Received(1).AddAsync(Arg.Is<FileEntity>(f => f.UploadedByUserId == "user-1" && f.ContentType == "application/pdf"));
    }

    [Fact]
    public async Task Upload_StripsAPathFromTheOriginalName()
    {
        var result = await _service.UploadFileAsync(Upload(@"..\..\secret\plan.pdf", Pdf), "user-1");

        Assert.Equal("plan.pdf", result.OriginalFileName);
    }

    [Theory]
    [InlineData("setup.exe")]
    [InlineData("page.html")]
    [InlineData("logo.svg")]
    [InlineData("noextension")]
    public async Task Upload_RefusesATypeThatIsNotOnTheAllowlist(string name)
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadFileAsync(Upload(name, Pdf), "user-1"));

        await _storage.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!);
    }

    [Fact]
    public async Task Upload_RefusesAProgramRenamedToAPdf()
    {
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadFileAsync(Upload("invoice.pdf", Exe), "user-1"));

        Assert.Contains("does not match", ex.Message);
        await _storage.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!);
    }

    [Fact]
    public async Task Upload_RefusesAFileOverTheLimit()
    {
        var dto = Upload("big.pdf", Pdf, length: FileRules.MaxFileSizeBytes + 1);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadFileAsync(dto, "user-1"));

        Assert.Contains("10 MB", ex.Message);
        await _storage.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!);
    }

    [Fact]
    public async Task Upload_AcceptsAFileExactlyAtTheLimit()
    {
        var dto = Upload("max.pdf", Pdf, length: FileRules.MaxFileSizeBytes);

        var result = await _service.UploadFileAsync(dto, "user-1");

        Assert.Equal(FileRules.MaxFileSizeBytes, result.SizeInBytes);
    }

    [Fact]
    public async Task Upload_RefusesAnEmptyFile()
        => await Assert.ThrowsAsync<ArgumentException>(() => _service.UploadFileAsync(Upload("empty.pdf", Array.Empty<byte>()), "user-1"));

    [Fact]
    public async Task Upload_RemovesTheStoredFile_WhenTheDatabaseFails()
    {
        _uow.CompleteAsync().Returns<Task<int>>(_ => throw new InvalidOperationException("db down"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => _service.UploadFileAsync(Upload("a.pdf", Pdf), "user-1"));

        await _storage.Received(1).DeleteAsync(Arg.Is<string>(n => n.EndsWith(".pdf")));
    }

    [Fact]
    public async Task Open_ReturnsTheStoredContent()
    {
        _files.GetByIdAsync(7).Returns(new FileEntity { Id = 7, StoredFileName = "x.pdf", OriginalFileName = "Brief.pdf", ContentType = "application/pdf" });
        _storage.OpenRead("x.pdf").Returns(new MemoryStream(Pdf));

        var content = await _service.OpenAsync(7);

        Assert.NotNull(content);
        Assert.Equal("Brief.pdf", content!.FileName);
        Assert.Equal("application/pdf", content.ContentType);
    }

    [Fact]
    public async Task Open_ReturnsNull_WhenTheRecordOrTheStoredFileIsMissing()
    {
        _files.GetByIdAsync(1).Returns((FileEntity?)null);
        _files.GetByIdAsync(2).Returns(new FileEntity { Id = 2, StoredFileName = "gone.pdf" });
        _storage.OpenRead("gone.pdf").Returns((Stream?)null);

        Assert.Null(await _service.OpenAsync(1));
        Assert.Null(await _service.OpenAsync(2));
    }
}
