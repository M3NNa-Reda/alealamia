using alealamia.Application.Services;
using alealamia.Domain.Enums;
using alealamia.Infrastructure.Persistence.Context;
using alealamia.Infrastructure.Repositories;
using alealamia.Integration.Tests.Support;
using Microsoft.EntityFrameworkCore;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Integration.Tests;

public class ConcurrentAssignmentTests : IClassFixture<SqlServerDatabaseFixture>
{
    private readonly SqlServerDatabaseFixture _database;

    public ConcurrentAssignmentTests(SqlServerDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task FiftyFreelancersAcceptingTheSameTask_ExactlyOneWins()
    {
        const int freelancers = 50;
        var scenario = await _database.SeedPublishedTaskAsync(freelancers);

        var outcomes = await AcceptConcurrentlyAsync(scenario.Offers);

        Assert.Empty(outcomes.Unexpected);
        Assert.Equal(1, outcomes.Winners);
        Assert.Equal(freelancers - 1, outcomes.Conflicts);

        await using var verify = _database.CreateContext();
        var task = await verify.Tasks.AsNoTracking().SingleAsync(t => t.Id == scenario.TaskId);
        Assert.Equal(TaskStatus.Assigned, task.Status);
        Assert.NotNull(task.AssignedFreelancerId);

        var offers = await verify.TaskOffers.AsNoTracking().Where(o => o.TaskId == scenario.TaskId).ToListAsync();
        var accepted = Assert.Single(offers, o => o.Status == TaskOfferStatus.Accepted);
        Assert.Equal(task.AssignedFreelancerId, accepted.FreelancerProfileId);
        Assert.Equal(freelancers - 1, offers.Count(o => o.Status == TaskOfferStatus.Closed));
        Assert.DoesNotContain(offers, o => o.Status == TaskOfferStatus.Pending);

        var assignedEntries = await verify.TaskStatusHistory
            .CountAsync(h => h.TaskId == scenario.TaskId && h.NewStatus == TaskStatus.Assigned);
        Assert.Equal(1, assignedEntries);
    }

    [SqlServerFact]
    public async System.Threading.Tasks.Task TheSameOfferAcceptedTwiceAtOnce_IsAppliedOnce()
    {
        var scenario = await _database.SeedPublishedTaskAsync(1);
        var offer = scenario.Offers.Single();

        var outcomes = await AcceptConcurrentlyAsync(new[] { offer, offer });

        Assert.Empty(outcomes.Unexpected);
        Assert.Equal(1, outcomes.Winners);
        Assert.Equal(1, outcomes.Conflicts);

        await using var verify = _database.CreateContext();
        var assignedEntries = await verify.TaskStatusHistory
            .CountAsync(h => h.TaskId == scenario.TaskId && h.NewStatus == TaskStatus.Assigned);
        Assert.Equal(1, assignedEntries);
    }

    private async Task<Outcomes> AcceptConcurrentlyAsync(IReadOnlyList<OfferInfo> offers)
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var attempts = offers
            .Select(offer => AcceptAsync(offer, gate.Task))
            .ToList();

        gate.SetResult();
        var results = await System.Threading.Tasks.Task.WhenAll(attempts);

        return new Outcomes(
            results.Count(r => r == Result.Won),
            results.Count(r => r == Result.Conflict),
            results.Where(r => r.Failure is not null).Select(r => r.Failure!).ToList());
    }

    private async Task<Result> AcceptAsync(OfferInfo offer, System.Threading.Tasks.Task start)
    {
        await start;

        await using var context = _database.CreateContext();
        var service = CreateService(context);

        try
        {
            await service.AcceptOfferAsync(offer.OfferId, offer.FreelancerProfileId);
            return Result.Won;
        }
        catch (InvalidOperationException)
        {
            return Result.Conflict;
        }
        catch (Exception exception)
        {
            return Result.Failed($"{exception.GetType().Name}: {exception.Message}");
        }
    }

    private static TaskAssignmentService CreateService(ApplicationDbContext context)
    {
        var tasks = new TaskRepository(context);
        var offers = new TaskOfferRepository(context);
        var history = new TaskStatusHistoryRepository(context);
        var eligibility = new TaskEligibilityService(
            tasks,
            new TaskRequiredSkillRepository(context),
            new TaskEligibilityRepository(context),
            offers,
            TimeProvider.System);

        return new TaskAssignmentService(tasks, offers, history, eligibility, new UnitOfWork(context), TimeProvider.System);
    }

    private sealed record Outcomes(int Winners, int Conflicts, IReadOnlyList<string> Unexpected);

    private sealed record Result(string Kind, string? Failure)
    {
        public static readonly Result Won = new("Won", null);
        public static readonly Result Conflict = new("Conflict", null);
        public static Result Failed(string message) => new("Failed", message);
    }
}
