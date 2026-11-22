using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using alealamia.Infrastructure.Identity.Entities;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TaskEntity = alealamia.Domain.Entities.Task;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Integration.Tests.Support;

public sealed record OfferInfo(int OfferId, int FreelancerProfileId);

public sealed record Scenario(int TaskId, IReadOnlyList<OfferInfo> Offers);

public sealed class SqlServerDatabaseFixture : IAsyncLifetime
{
    private readonly string _connectionString;

    public SqlServerDatabaseFixture()
    {
        var builder = new SqlConnectionStringBuilder(SqlServerAvailability.BaseConnectionString)
        {
            InitialCatalog = $"AlealamiaDB_Test_{Guid.NewGuid():N}"
        };
        _connectionString = builder.ConnectionString;
    }

    public ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(_connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }

    public async System.Threading.Tasks.Task InitializeAsync()
    {
        if (!SqlServerAvailability.IsAvailable)
            return;

        await using var context = CreateContext();
        await context.Database.MigrateAsync();
    }

    public async System.Threading.Tasks.Task DisposeAsync()
    {
        if (!SqlServerAvailability.IsAvailable)
            return;

        SqlConnection.ClearAllPools();
        await using var context = CreateContext();
        await context.Database.EnsureDeletedAsync();
    }

    public async Task<Scenario> SeedPublishedTaskAsync(int freelancerCount)
    {
        await using var context = CreateContext();
        var now = DateTime.UtcNow;

        var admin = NewUser("admin");
        var clientUser = NewUser("client");
        var freelancerUsers = Enumerable.Range(1, freelancerCount).Select(i => NewUser($"freelancer{i}")).ToList();

        context.Users.AddRange(admin, clientUser);
        context.Users.AddRange(freelancerUsers);

        var track = new Track { Name = $"Track {Guid.NewGuid():N}", IsActive = true };
        var client = new ClientProfile { UserId = clientUser.Id };
        context.AddRange(track, client);
        await context.SaveChangesAsync();

        var project = new Project
        {
            ClientProfileId = client.Id,
            Title = "Integration project",
            Description = "Project used by the concurrency tests.",
            Status = ProjectStatus.InProgress,
            Currency = "EGP",
            CreatedAt = now
        };
        var profiles = freelancerUsers
            .Select(user => new FreelancerProfile
            {
                UserId = user.Id,
                TrackId = track.Id,
                ApprovalStatus = FreelancerApprovalStatus.Approved,
                IsAvailable = true
            })
            .ToList();

        context.Projects.Add(project);
        context.FreelancerProfiles.AddRange(profiles);
        await context.SaveChangesAsync();

        var task = new TaskEntity
        {
            ProjectId = project.Id,
            TrackId = track.Id,
            Title = "Contested task",
            Description = "Every freelancer receives an offer for this task.",
            Status = TaskStatus.Published,
            PublishedDeadline = now.AddDays(5),
            CreatedByUserId = admin.Id,
            CreatedAt = now
        };
        context.Tasks.Add(task);
        await context.SaveChangesAsync();

        var offers = profiles
            .Select(profile => new TaskOffer
            {
                TaskId = task.Id,
                FreelancerProfileId = profile.Id,
                Status = TaskOfferStatus.Pending,
                SentAt = now
            })
            .ToList();
        context.TaskOffers.AddRange(offers);
        await context.SaveChangesAsync();

        return new Scenario(
            task.Id,
            offers.Select(o => new OfferInfo(o.Id, o.FreelancerProfileId)).ToList());
    }

    private static ApplicationUser NewUser(string name)
    {
        var id = Guid.NewGuid().ToString();
        var userName = $"{name}-{id}";

        return new ApplicationUser
        {
            Id = id,
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            Email = $"{userName}@example.test",
            NormalizedEmail = $"{userName}@example.test".ToUpperInvariant(),
            FirstName = name,
            LastName = "Test",
            IsActive = true,
            SecurityStamp = Guid.NewGuid().ToString()
        };
    }
}
