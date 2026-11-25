using System.Net;
using System.Net.Http.Headers;
using alealamia.Application.DTOs.Auth;
using alealamia.Application.DTOs.Freelancer;
using alealamia.Domain.Constants;
using alealamia.Domain.Enums;
using alealamia.Infrastructure.Identity.Entities;
using alealamia.Infrastructure.Persistence.Context;
using alealamia.Infrastructure.Seeders;
using EasyShop.API;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using ClientProfileEntity = alealamia.Domain.Entities.ClientProfile;
using ProjectEntity = alealamia.Domain.Entities.Project;

namespace alealamia.Integration.Tests.Support;

public sealed record World(int TrackId, int SkillId);

public sealed record FreelancerAccount(HttpClient Http, int ProfileId, string Email, string UserId);

public sealed record NamedItem(int Id, string Name);

public sealed record ProfileSummary(int Id, int TrackId);

/// <summary>
/// Starts the real API (real DI, middleware, JWT, controllers) against a throw-away SQL Server database
/// that is built from the real migrations. One instance is shared by every end-to-end test class.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    public const string AdminEmail = "admin@e2e.test";
    public const string AdminPassword = "E2eAdmin@123";
    public const string UserPassword = "E2e-Pass@123";

    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public ApiFixture()
    {
        var builder = new SqlConnectionStringBuilder(SqlServerAvailability.BaseConnectionString)
        {
            InitialCatalog = $"AlealamiaDB_E2E_{Guid.NewGuid():N}"
        };
        _connectionString = builder.ConnectionString;
    }

    public HttpClient Admin { get; private set; } = null!;

    public int ProjectId { get; private set; }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    public HttpClient CreateAnonymousClient() => _factory!.CreateClient();

    public async System.Threading.Tasks.Task InitializeAsync()
    {
        if (!SqlServerAvailability.IsAvailable)
            return;

        await using (var context = CreateContext())
        {
            await context.Database.MigrateAsync();
        }

        ConfigureEnvironment();
        _factory = new WebApplicationFactory<Program>();

        // Idempotent: makes sure the roles and the admin exist even if the host skipped the seeding in Program.cs.
        using (var scope = _factory.Services.CreateScope())
        {
            await RoleSeeder.SeedAsync(
                scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>(),
                scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>(),
                scope.ServiceProvider.GetRequiredService<IConfiguration>());
        }

        Admin = await LoginAsync(AdminEmail, AdminPassword);
        ProjectId = await CreateProjectAsync();
    }

    public async System.Threading.Tasks.Task DisposeAsync()
    {
        Admin?.Dispose();
        _factory?.Dispose();

        if (!SqlServerAvailability.IsAvailable)
            return;

        SqlConnection.ClearAllPools();
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    public async Task<HttpClient> LoginAsync(string email, string password)
    {
        var client = _factory!.CreateClient();

        var response = await client.PostJsonAsync("api/Auth/login", new LoginRequestDto { Email = email, Password = password });
        await response.ExpectAsync(HttpStatusCode.OK);

        var result = await response.ReadAsync<AuthResultDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Data!.Token);
        return client;
    }

    /// <summary>Registers an account through the public endpoint and returns a client that is already signed in.</summary>
    public async Task<(HttpClient Http, string UserId, string Email)> RegisterAsync(string role, string? email = null)
    {
        email ??= $"{role.ToLowerInvariant()}.{Guid.NewGuid():N}@e2e.test";
        var client = _factory!.CreateClient();

        var response = await client.PostJsonAsync("api/Auth/register", new RegisterRequestDto
        {
            FirstName = "E2E",
            LastName = role,
            Email = email,
            Password = UserPassword,
            Role = role
        });
        await response.ExpectAsync(HttpStatusCode.OK);

        var result = await response.ReadAsync<AuthResultDto>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Data!.Token);
        return (client, result.Data.User.UserId, email);
    }

    /// <summary>Creates a track and a skill that belongs to it, so every test works in its own isolated track.</summary>
    public async Task<World> CreateWorldAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..10];
        var trackName = $"E2E Track {suffix}";
        var skillName = $"E2E Skill {suffix}";

        await (await Admin.PostJsonAsync("api/Track", new { name = trackName, description = "End-to-end track" }))
            .ExpectAsync(HttpStatusCode.OK);
        await (await Admin.PostJsonAsync("api/Skill", new { name = skillName, description = "End-to-end skill" }))
            .ExpectAsync(HttpStatusCode.OK);

        var tracks = await (await Admin.GetAsync("api/Track/admin")).ExpectAsync(HttpStatusCode.OK);
        var track = (await tracks.ReadListAsync<NamedItem>()).Single(t => t.Name == trackName);

        var skills = await (await Admin.GetAsync("api/Skill/admin")).ExpectAsync(HttpStatusCode.OK);
        var skill = (await skills.ReadListAsync<NamedItem>()).Single(s => s.Name == skillName);

        await (await Admin.PostJsonAsync($"api/Skill/track/{track.Id}/{skill.Id}", null))
            .ExpectAsync(HttpStatusCode.OK);

        return new World(track.Id, skill.Id);
    }

    /// <summary>
    /// Registers a freelancer, completes the profile through the API and (optionally) approves the freelancer.
    /// Availability is switched on in the database because the API has no endpoint for it yet.
    /// </summary>
    public async Task<FreelancerAccount> CreateFreelancerAsync(World world, bool approve = true, bool available = true)
    {
        var (http, userId, email) = await RegisterAsync(AppRoles.Freelancer);

        await (await http.PostJsonAsync("api/FreelancerProfile", new CompleteFreelancerProfileDto
        {
            TrackId = world.TrackId,
            Title = "E2E freelancer",
            About = "Created by the end-to-end tests.",
            SkillIds = new List<int> { world.SkillId }
        })).ExpectAsync(HttpStatusCode.OK);

        var profile = await http.GetAsAsync<ProfileSummary>("api/FreelancerProfile/profile");

        if (approve)
        {
            await (await Admin.PatchEmptyAsync($"api/admin/freelancers/{profile.Id}/approve"))
                .ExpectAsync(HttpStatusCode.OK);
        }

        if (available)
        {
            await using var context = CreateContext();
            await context.FreelancerProfiles
                .Where(p => p.Id == profile.Id)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsAvailable, true));
        }

        return new FreelancerAccount(http, profile.Id, email, userId);
    }

    private async Task<int> CreateProjectAsync()
    {
        var (http, userId, _) = await RegisterAsync(AppRoles.Client);
        http.Dispose();

        await using var context = CreateContext();

        var client = new ClientProfileEntity { UserId = userId };
        context.Add(client);
        await context.SaveChangesAsync();

        var project = new ProjectEntity
        {
            ClientProfileId = client.Id,
            Title = "E2E project",
            Description = "Project used by the end-to-end tests.",
            Status = ProjectStatus.InProgress,
            Currency = "EGP",
            CreatedAt = DateTime.UtcNow
        };
        context.Add(project);
        await context.SaveChangesAsync();

        return project.Id;
    }

    private void ConfigureEnvironment()
    {
        // Environment variables are read by WebApplication.CreateBuilder and win over user secrets and appsettings.
        Set("ConnectionStrings__ConnectionString", _connectionString);
        Set("Jwt__Key", "e2e-only-signing-key-0123456789-abcdefghijklmnopqrstuvwxyz");
        Set("Jwt__Issuer", "AlalamiaApi.E2E");
        Set("Jwt__Audience", "AlalamiaClient.E2E");
        Set("Jwt__AccessTokenExpirationMinutes", "30");
        Set("AdminSettings__FirstName", "E2E");
        Set("AdminSettings__LastName", "Admin");
        Set("AdminSettings__Email", AdminEmail);
        Set("AdminSettings__Password", AdminPassword);

        static void Set(string name, string value) => Environment.SetEnvironmentVariable(name, value);
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFixture>
{
    public const string Name = "api";
}
