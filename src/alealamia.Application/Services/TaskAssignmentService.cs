using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using Task = System.Threading.Tasks.Task;
using TaskEntity = alealamia.Domain.Entities.Task;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.Services;

public class TaskAssignmentService : ITaskAssignmentService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskOfferRepository _offerRepository;
    private readonly ITaskStatusHistoryRepository _historyRepository;
    private readonly ITaskEligibilityService _eligibilityService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public TaskAssignmentService(
        ITaskRepository taskRepository,
        ITaskOfferRepository offerRepository,
        ITaskStatusHistoryRepository historyRepository,
        ITaskEligibilityService eligibilityService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _taskRepository = taskRepository;
        _offerRepository = offerRepository;
        _historyRepository = historyRepository;
        _eligibilityService = eligibilityService;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<TaskAssignmentResultDto> AcceptOfferAsync(int offerId, int freelancerProfileId)
    {
        var offer = await _offerRepository.GetDetailsByIdAsync(offerId)
            ?? throw new KeyNotFoundException($"Offer {offerId} was not found.");

        if (offer.FreelancerProfileId != freelancerProfileId)
            throw new UnauthorizedAccessException("This offer was not sent to the freelancer.");

        if (offer.Status != TaskOfferStatus.Pending)
            throw new InvalidOperationException($"This offer is no longer available ({offer.Status}).");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        EnsureOpenForAssignment(offer.Task, now);

        var eligibility = await _eligibilityService.CheckAsync(offer.TaskId, freelancerProfileId);
        if (!eligibility.IsEligible)
            throw new InvalidOperationException(string.Join(" ", eligibility.Reasons));

        return await ExecuteAssignmentAsync(
            offer.TaskId,
            freelancerProfileId,
            offerId,
            eligibility.FreelancerUserId,
            "Offer accepted by the freelancer.",
            now);
    }

    public async Task<TaskAssignmentResultDto> AssignAsync(AssignTaskDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.AssignedByUserId))
            throw new ArgumentException("AssignedByUserId is required.");

        var task = await _taskRepository.GetReadOnlyByIdAsync(dto.TaskId)
            ?? throw new KeyNotFoundException($"Task {dto.TaskId} was not found.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        EnsureOpenForAssignment(task, now);

        var eligibility = await _eligibilityService.CheckAsync(dto.TaskId, dto.FreelancerProfileId);
        if (!eligibility.IsEligible)
            throw new InvalidOperationException(string.Join(" ", eligibility.Reasons));

        return await ExecuteAssignmentAsync(
            dto.TaskId,
            dto.FreelancerProfileId,
            null,
            dto.AssignedByUserId,
            "Task assigned manually by an administrator.",
            now);
    }

    public async Task<TaskAssignmentDto?> GetCurrentAsync(int taskId)
    {
        var task = await _taskRepository.GetReadOnlyByIdAsync(taskId)
            ?? throw new KeyNotFoundException($"Task {taskId} was not found.");

        if (task.AssignedFreelancerId is not { } freelancerId)
            return null;

        var assigned = await _historyRepository.GetLatestAsync(taskId, TaskStatus.Assigned);

        return new TaskAssignmentDto
        {
            TaskId = task.Id,
            FreelancerProfileId = freelancerId,
            Status = task.Status,
            AssignedAt = assigned?.ChangedAt,
            PublishedDeadline = task.PublishedDeadline,
            ExtensionDeadline = task.ExtensionDeadline
        };
    }

    private async Task<TaskAssignmentResultDto> ExecuteAssignmentAsync(
        int taskId,
        int freelancerProfileId,
        int? offerId,
        string changedByUserId,
        string notes,
        DateTime now)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var assigned = await _taskRepository.TryAssignAsync(taskId, freelancerProfileId, now);
            if (!assigned)
                throw new InvalidOperationException("The task has already been taken by another freelancer.");

            TaskOffer? created = null;
            if (offerId.HasValue)
            {
                var accepted = await _offerRepository.TryAcceptAsync(offerId.Value, now);
                if (!accepted)
                    throw new InvalidOperationException("This offer is no longer available.");
            }
            else
            {
                created = await _offerRepository.GetOrCreateAcceptedAsync(taskId, freelancerProfileId, now);
            }

            var closed = await _offerRepository.ClosePendingAsync(taskId, offerId ?? created?.Id, now);

            await _historyRepository.AddAsync(new TaskStatusHistory
            {
                TaskId = taskId,
                OldStatus = TaskStatus.Published,
                NewStatus = TaskStatus.Assigned,
                ChangedByUserId = changedByUserId,
                Notes = notes,
                ChangedAt = now
            });

            await _unitOfWork.CompleteAsync();
            await _unitOfWork.CommitTransactionAsync();

            return new TaskAssignmentResultDto
            {
                TaskId = taskId,
                FreelancerProfileId = freelancerProfileId,
                OfferId = offerId ?? created!.Id,
                Status = TaskStatus.Assigned,
                AssignedAt = now,
                ClosedOfferCount = closed
            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private static void EnsureOpenForAssignment(TaskEntity task, DateTime now)
    {
        if (task.Status != TaskStatus.Published || task.AssignedFreelancerId is not null)
            throw new InvalidOperationException("This task is no longer available.");

        if (task.PublishedDeadline <= now)
            throw new InvalidOperationException("The task deadline has already passed.");
    }
}
