using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Constants;
using alealamia.Domain.Enums;
using TaskEntity = alealamia.Domain.Entities.Task;

namespace alealamia.Application.Services;

public class TaskEligibilityService : ITaskEligibilityService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskRequiredSkillRepository _requiredSkillRepository;
    private readonly ITaskEligibilityRepository _eligibilityRepository;
    private readonly ITaskOfferRepository _offerRepository;
    private readonly TimeProvider _timeProvider;

    public TaskEligibilityService(
        ITaskRepository taskRepository,
        ITaskRequiredSkillRepository requiredSkillRepository,
        ITaskEligibilityRepository eligibilityRepository,
        ITaskOfferRepository offerRepository,
        TimeProvider timeProvider)
    {
        _taskRepository = taskRepository;
        _requiredSkillRepository = requiredSkillRepository;
        _eligibilityRepository = eligibilityRepository;
        _offerRepository = offerRepository;
        _timeProvider = timeProvider;
    }

    public async Task<TaskEligibilityDto> CheckAsync(int taskId, int freelancerProfileId)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var task = await GetTaskOrThrowAsync(taskId);
        var requiredSkillIds = await GetRequiredSkillIdsAsync(taskId);

        var snapshot = await _eligibilityRepository.GetFreelancerSnapshotAsync(freelancerProfileId, now)
            ?? throw new KeyNotFoundException($"Freelancer {freelancerProfileId} was not found.");

        var acceptedThisMonth = await _offerRepository.CountAcceptedSinceAsync(freelancerProfileId, MonthStart(now));
        return Evaluate(task, snapshot, requiredSkillIds, acceptedThisMonth);
    }

    public async Task<IEnumerable<EligibleFreelancerDto>> GetEligibleFreelancersAsync(int taskId)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var task = await GetTaskOrThrowAsync(taskId);
        var requiredSkillIds = await GetRequiredSkillIdsAsync(taskId);

        var candidates = await _eligibilityRepository.GetCandidatesAsync(task.TrackId, now);
        var qualified = candidates
            .Where(c => requiredSkillIds.All(c.SkillIds.Contains))
            .ToList();

        if (qualified.Count == 0)
            return Array.Empty<EligibleFreelancerDto>();

        var counts = await _offerRepository.GetAcceptedCountsSinceAsync(
            qualified.Select(c => c.FreelancerProfileId).ToList(),
            MonthStart(now));

        return qualified
            .Select(c => new EligibleFreelancerDto
            {
                FreelancerProfileId = c.FreelancerProfileId,
                UserId = c.UserId,
                TrackId = c.TrackId,
                AcceptedThisMonth = counts.GetValueOrDefault(c.FreelancerProfileId),
                RemainingThisMonth = TaskRules.MonthlyAcceptedLimit - counts.GetValueOrDefault(c.FreelancerProfileId)
            })
            .Where(e => e.RemainingThisMonth > 0)
            .OrderBy(e => e.AcceptedThisMonth)
            .ThenBy(e => e.FreelancerProfileId)
            .ToList();
    }

    public async Task<FreelancerTaskLimitDto> GetMonthlyLimitAsync(int freelancerProfileId)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        _ = await _eligibilityRepository.GetFreelancerSnapshotAsync(freelancerProfileId, now)
            ?? throw new KeyNotFoundException($"Freelancer {freelancerProfileId} was not found.");

        var periodStart = MonthStart(now);
        var accepted = await _offerRepository.CountAcceptedSinceAsync(freelancerProfileId, periodStart);

        return new FreelancerTaskLimitDto
        {
            FreelancerProfileId = freelancerProfileId,
            MonthlyLimit = TaskRules.MonthlyAcceptedLimit,
            AcceptedThisMonth = accepted,
            Remaining = Math.Max(0, TaskRules.MonthlyAcceptedLimit - accepted),
            PeriodStart = periodStart,
            PeriodEnd = periodStart.AddMonths(1)
        };
    }

    private static TaskEligibilityDto Evaluate(
        TaskEntity task,
        FreelancerEligibilitySnapshot freelancer,
        IReadOnlyCollection<int> requiredSkillIds,
        int acceptedThisMonth)
    {
        var reasons = new List<string>();

        if (freelancer.ApprovalStatus != FreelancerApprovalStatus.Approved)
            reasons.Add("The freelancer has not been approved.");

        if (freelancer.TrackId != task.TrackId)
            reasons.Add("The freelancer belongs to a different track.");

        if (!freelancer.IsAvailable)
            reasons.Add("The freelancer is currently unavailable.");

        if (freelancer.ActiveBlockEndsAt is { } blockEnds)
            reasons.Add($"The freelancer is blocked until {blockEnds:yyyy-MM-dd HH:mm} UTC.");

        var missingSkillIds = requiredSkillIds.Where(id => !freelancer.SkillIds.Contains(id)).ToList();
        if (missingSkillIds.Count > 0)
            reasons.Add($"The freelancer is missing {missingSkillIds.Count} required skill(s).");

        if (acceptedThisMonth >= TaskRules.MonthlyAcceptedLimit)
            reasons.Add($"The monthly limit of {TaskRules.MonthlyAcceptedLimit} accepted tasks has been reached.");

        return new TaskEligibilityDto
        {
            TaskId = task.Id,
            FreelancerProfileId = freelancer.FreelancerProfileId,
            FreelancerUserId = freelancer.UserId,
            IsEligible = reasons.Count == 0,
            Reasons = reasons,
            MissingSkillIds = missingSkillIds,
            AcceptedThisMonth = acceptedThisMonth,
            MonthlyLimit = TaskRules.MonthlyAcceptedLimit
        };
    }

    private async Task<TaskEntity> GetTaskOrThrowAsync(int taskId)
    {
        return await _taskRepository.GetReadOnlyByIdAsync(taskId)
            ?? throw new KeyNotFoundException($"Task {taskId} was not found.");
    }

    private async Task<IReadOnlyCollection<int>> GetRequiredSkillIdsAsync(int taskId)
    {
        var skills = await _requiredSkillRepository.GetByTaskIdAsync(taskId);
        return skills.Select(s => s.SkillId).ToList();
    }

    private static DateTime MonthStart(DateTime utcNow) =>
        new(utcNow.Year, utcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);
}
