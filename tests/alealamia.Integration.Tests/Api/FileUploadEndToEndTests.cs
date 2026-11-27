using System.Net;
using System.Net.Http.Headers;
using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Files;
using alealamia.Application.DTOs.TaskOffers;
using alealamia.Application.DTOs.Tasks;
using alealamia.Domain.Constants;
using alealamia.Integration.Tests.Support;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Integration.Tests.Api;

/// <summary>
/// Secure upload and authorized download: type, size and content are checked, only signed-in staff and freelancers
/// can upload, a file is only downloadable through the task it is attached to, and uploads are rate limited.
/// </summary>
[Collection(ApiCollection.Name)]
public class FileUploadEndToEndTests
{
    private static readonly byte[] Pdf = "%PDF-1.4\n1 0 obj\n<<>>\nendobj\n"u8.ToArray();
    private static readonly byte[] Executable = { 0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00 };

    private readonly ApiFixture _api;

    public FileUploadEndToEndTests(ApiFixture api)
    {
        _api = api;
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task AFreelancer_CanUploadAPdf_AndTheServerDecidesTheContentType()
    {
        var freelancer = await _api.CreateFreelancerAsync(await _api.CreateWorldAsync());

        var response = await freelancer.Http.PostAsync("api/files", Form("Brief.PDF", Pdf, "text/html"));
        await response.ExpectAsync(HttpStatusCode.OK);
        var file = await response.ReadAsync<FileDto>();

        Assert.Equal("Brief.PDF", file.OriginalFileName);
        Assert.Equal("application/pdf", file.ContentType);
        Assert.Equal(Pdf.Length, file.SizeInBytes);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Upload_RefusesADangerousExtension_AndAProgramRenamedToAPdf()
    {
        var freelancer = await _api.CreateFreelancerAsync(await _api.CreateWorldAsync());

        await (await freelancer.Http.PostAsync("api/files", Form("setup.exe", Executable)))
            .ExpectAsync(HttpStatusCode.BadRequest);
        await (await freelancer.Http.PostAsync("api/files", Form("page.html", "<script>alert(1)</script>"u8.ToArray())))
            .ExpectAsync(HttpStatusCode.BadRequest);
        await (await freelancer.Http.PostAsync("api/files", Form("invoice.pdf", Executable)))
            .ExpectAsync(HttpStatusCode.BadRequest);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Upload_RefusesAFileOverTenMegabytes()
    {
        var freelancer = await _api.CreateFreelancerAsync(await _api.CreateWorldAsync());
        var tooBig = new byte[10 * 1024 * 1024 + 512 * 1024];
        Pdf.CopyTo(tooBig, 0);

        await (await freelancer.Http.PostAsync("api/files", Form("big.pdf", tooBig)))
            .ExpectAsync(HttpStatusCode.BadRequest);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Upload_NeedsASignedInFreelancerOrAdmin()
    {
        using var anonymous = _api.CreateAnonymousClient();
        await (await anonymous.PostAsync("api/files", Form("a.pdf", Pdf))).ExpectAsync(HttpStatusCode.Unauthorized);

        var (client, _, _) = await _api.RegisterAsync(AppRoles.Client);
        await (await client.PostAsync("api/files", Form("a.pdf", Pdf))).ExpectAsync(HttpStatusCode.Forbidden);

        await (await _api.Admin.PostAsync("api/files", Form("a.pdf", Pdf))).ExpectAsync(HttpStatusCode.OK);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Download_IsOnlyForTheAssignedFreelancerAndAdmins_AndIsSentAsAnAttachment()
    {
        var world = await _api.CreateWorldAsync();
        var assigned = await _api.CreateFreelancerAsync(world);
        // A freelancer in another track never receives an offer for this task, so has no access to its files.
        var stranger = await _api.CreateFreelancerAsync(await _api.CreateWorldAsync());
        var task = await AssignTaskAsync(world, assigned);

        var file = await UploadAsync(assigned.Http, "Delivery.pdf", Pdf);
        await (await assigned.Http.PostJsonAsync($"api/tasks/{task.Id}/files", new { fileId = file.Id }))
            .ExpectAsync(HttpStatusCode.Created);

        var url = $"api/tasks/{task.Id}/files/{file.Id}/download";

        var own = await assigned.Http.GetAsync(url);
        await own.ExpectAsync(HttpStatusCode.OK);
        Assert.Equal(Pdf, await own.Content.ReadAsByteArrayAsync());
        Assert.Equal("application/pdf", own.Content.Headers.ContentType?.MediaType);
        Assert.Equal("attachment", own.Content.Headers.ContentDisposition?.DispositionType);
        Assert.Equal("nosniff", own.Headers.GetValues("X-Content-Type-Options").Single());

        await (await _api.Admin.GetAsync(url)).ExpectAsync(HttpStatusCode.OK);
        await (await stranger.Http.GetAsync(url)).ExpectAsync(HttpStatusCode.NotFound);

        using var anonymous = _api.CreateAnonymousClient();
        await (await anonymous.GetAsync(url)).ExpectAsync(HttpStatusCode.Unauthorized);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Download_OfAFileThatIsNotAttachedToTheTask_IsNotFound()
    {
        var world = await _api.CreateWorldAsync();
        var assigned = await _api.CreateFreelancerAsync(world);
        var task = await AssignTaskAsync(world, assigned);
        var loose = await UploadAsync(assigned.Http, "Loose.pdf", Pdf);

        await (await assigned.Http.GetAsync($"api/tasks/{task.Id}/files/{loose.Id}/download"))
            .ExpectAsync(HttpStatusCode.NotFound);
        await (await _api.Admin.GetAsync($"api/tasks/{task.Id}/files/{loose.Id}/download"))
            .ExpectAsync(HttpStatusCode.NotFound);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task Uploads_AreRateLimited()
    {
        using var factory = _api.CreateFactory(("RateLimiting:Upload:PermitLimit", "2"));
        using var client = factory.CreateClient();

        var first = await client.PostAsync("api/files", Form("a.pdf", Pdf));
        var second = await client.PostAsync("api/files", Form("a.pdf", Pdf));
        var third = await client.PostAsync("api/files", Form("a.pdf", Pdf));

        Assert.Equal(HttpStatusCode.Unauthorized, first.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, second.StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, third.StatusCode);
    }

    // ---------- Helpers ----------

    private static MultipartFormDataContent Form(string fileName, byte[] bytes, string contentType = "application/octet-stream")
    {
        var part = new ByteArrayContent(bytes);
        part.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var form = new MultipartFormDataContent();
        form.Add(part, "File", fileName);
        return form;
    }

    private static async Task<FileDto> UploadAsync(HttpClient client, string fileName, byte[] bytes)
    {
        var response = await client.PostAsync("api/files", Form(fileName, bytes));
        await response.ExpectAsync(HttpStatusCode.OK);
        return await response.ReadAsync<FileDto>();
    }

    private async Task<TaskDto> AssignTaskAsync(World world, FreelancerAccount freelancer)
    {
        var created = await _api.Admin.PostJsonAsync("api/admin/tasks", new
        {
            projectId = _api.ProjectId,
            trackId = world.TrackId,
            title = "E2E file task",
            description = "Created by the file end-to-end tests.",
            rewardAmount = 1000m,
            publishedDeadline = DateTime.UtcNow.AddDays(7),
            requiredSkillIds = new[] { world.SkillId }
        });
        await created.ExpectAsync(HttpStatusCode.Created);
        var task = await created.ReadAsync<TaskDto>();

        await (await _api.Admin.PatchJsonAsync($"api/admin/tasks/{task.Id}/status", new { newStatus = (int)TaskStatus.Published }))
            .ExpectAsync(HttpStatusCode.OK);

        var offers = await freelancer.Http.GetAsAsync<PagedResultDto<TaskOfferDetailsDto>>("api/task-offers/my-offers");
        var offer = Assert.Single(offers.Items, o => o.TaskId == task.Id);
        await (await freelancer.Http.PostJsonAsync($"api/task-offers/{offer.Id}/respond", new { accept = true }))
            .ExpectAsync(HttpStatusCode.OK);

        return task;
    }
}
