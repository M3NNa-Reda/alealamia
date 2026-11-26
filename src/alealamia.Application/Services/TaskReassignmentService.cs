using alealamia.Application.DTOs.FreelancerBlock;
using alealamia.Application.DTOs.TaskAssignments;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Application.Mappings;
using alealamia.Domain.Constants;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using Task = System.Threading.Tasks.Task;
using TaskEntity = alealamia.Domain.Entities.Task;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.Services;

public class TaskReassignmentService : ITaskReassignmentService
{
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskReassignmentRepository _reassignmentRepository;
    private readonly ITaskOfferRepository _offerRepository;
    private readonly ITaskStatusHistoryRepository _historyRepository;
    private readonly ITaskEligibilityRepository _eligibilityRepository;
    private readonly ITaskEligibilityService _eligibilityService;
    private readonly ITaskOfferService _offerService;
    private readonly IFreelancerBlockService _blockService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public TaskReassignmentService(
        ITaskRepository taskRepository,
        ITaskReassignmentRepository reassignmentRepository,
        ITaskOfferRepository offerRepository,
        ITaskStatusHistoryRepository historyRepository,
        ITaskEligibilityRepository eligibilityRepository,
        ITaskEligibilityService eligibilityService,
        ITaskOfferService offerService,
        IFreelancerBlockService blockService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _taskRepository = taskRepository;
        _reassignmentRepository = reassignmentRepository;
        _offerRepository = offerRepository;
        _historyRepository = historyRepository;
        _eligibilityRepository = eligibilityRepository;
        _eligibilityService = eligibilityService;
        _offerService = offerService;
        _blockService = blockService;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<TaskReassignmentDto> ReassignAsync(CreateTaskReassignmentDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.ReassignedByUserId))
            throw new ArgumentException("ReassignedByUserId is required.");
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new ArgumentException("A reason for the reassignment is required.");

        var task = await GetAssignedTaskAsync(dto.TaskId);

        if (dto.NewFreelancerProfileId == task.AssignedFreelancerId)
            throw new InvalidOperationException("The task is already assigned to this freelancer.");

        if (dto.NewFreelancerProfileId is { } newFreelancerId)
        {
            var eligibility = await _eligibilityService.CheckAsync(task.Id, newFreelancerId);
            if (!eligibility.IsEligible)
                throw new InvalidOperationException(string.Join(" ", eligibility.Reasons));
        }

        return await ExecuteAsync(
            task,
            dto.NewFreelancerProfileId,
            dto.Reason.Trim(),
            dto.ReassignedByUserId);
    }

    public async Task<TaskReassignmentDto> WithdrawAsync(WithdrawTaskDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new ArgumentException("A reason for the withdrawal is required.");

        var task = await GetAssignedTaskAsync(dto.TaskId);

        if (task.AssignedFreelancerId != dto.FreelancerProfileId)
            throw new UnauthorizedAccessException("The task is not assigned to this freelancer.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var freelancer = await _eligibilityRepository.GetFreelancerSnapshotAsync(dto.FreelancerProfileId, now)
            ?? throw new KeyNotFoundException($"Freelancer {dto.FreelancerProfileId} was not found.");

        return await ExecuteAsync(
            task,
            null,
            dto.Reason.Trim(),
            freelancer.UserId,
            new BlockSpec(TaskRules.WithdrawalBlockDays, $"Withdrew from task #{task.Id} after accepting it."));
    }

    public async Task<TaskReassignmentDto> RecordNoShowAsync(int taskId)
    {
        var task = await GetAssignedTaskAsync(taskId);

        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var finalDeadline = task.ExtensionDeadline ?? task.PublishedDeadline.AddDays(TaskRules.LateGraceDays);
        if (finalDeadline >= now)
            throw new InvalidOperationException("The task has not passed its final deadline.");

        var freelancerId = task.AssignedFreelancerId!.Value;
        var freelancer = await _eligibilityRepository.GetFreelancerSnapshotAsync(freelancerId, now)
            ?? throw new KeyNotFoundException($"Freelancer {freelancerId} was not found.");

        // The freelancer's own account is recorded as the actor because the history tables require a real user.
        return await ExecuteAsync(
            task,
            null,
            "Automatic: no delivery before the final deadline.",
            freelancer.UserId,
            new BlockSpec(TaskRules.NoShowBlockDays, $"No delivery for task #{task.Id} before the final deadline."));
    }

    public async Task<IEnumerable<TaskReassignmentDto>> GetByTaskAsync(int taskId)
    {
        var reassignments = await _reassignmentRepository.GetByTaskIdAsync(taskId);
        return reassignments.Select(r => r.ToDto()).ToList();
    }

    private async Task<TaskReassignmentDto> ExecuteAsync(
        TaskEntity task,
        int? newFreelancerProfileId,
        string reason,
        string changedByUserId,
        BlockSpec? block = null)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var previousFreelancerId = task.AssignedFreelancerId!.Value;
        var resultingStatus = newFreelancerProfileId.HasValue ? TaskStatus.Assigned : TaskStatus.Published;

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var changed = await _taskRepository.TryReassignAsync(
                task.Id,
                previousFreelancerId,
                newFreelancerProfileId,
                resultingStatus,
                now);

            if (!changed)
                throw new InvalidOperationException("The task assignment was changed by someone else. Reload and try again.");

            if (newFreelancerProfileId.HasValue)
                await _offerRepository.GetOrCreateAcceptedAsync(task.Id, newFreelancerProfileId.Value, now);

            var reassignment = new TaskReassignment
            {
                TaskId = task.Id,
                PreviousFreelancerId = previousFreelancerId,
                NewFreelancerId = newFreelancerProfileId,
                Reason = reason,
                ReassignedByUserId = changedByUserId,
                ReassignedAt = now
            };
            await _reassignmentRepository.AddAsync(reassignment);

            if (!newFreelancerProfileId.HasValue)
            {
                await _historyRepository.AddAsync(new TaskStatusHistory
                {
                    TaskId = task.Id,
                    OldStatus = TaskStatus.Assigned,
                    NewStatus = TaskStatus.Published,
                    ChangedByUserId = changedByUserId,
                    Notes = reason,
                    ChangedAt = now
                });
            }

            // The block is written before the new offers are sent, so the freelancer who just lost the task
            // is not offered it again. It shares the transaction: if it fails, the whole reassignment is rolled back.
            if (block is not null)
            {
                var blockResult = await _blockService.CreateBlockAsync(new CreateFreelancerBlockDto
                {
                    FreelancerProfileId = previousFreelancerId,
                    TaskId = task.Id,
                    Reason = block.Reason,
                    StartedAt = now,
                    EndsAt = now.AddDays(block.Days),
                    CreatedByUserId = changedByUserId
                });

                if (!blockResult.Succeeded)
                    throw new InvalidOperationException(string.Join(" ", blockResult.Errors));
            }

            await _unitOfWork.CompleteAsync();

            if (!newFreelancerProfileId.HasValue)
                await _offerService.SendOffersAsync(task.Id);

            await _unitOfWork.CommitTransactionAsync();
            return reassignment.ToDto();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    private sealed record BlockSpec(int Days, string Reason);

    private async Task<TaskEntity> GetAssignedTaskAsync(int taskId)
    {
        var task = await _taskRepository.GetReadOnlyByIdAsync(taskId)
            ?? throw new KeyNotFoundException($"Task {taskId} was not found.");

        if (task.Status != TaskStatus.Assigned || task.AssignedFreelancerId is null)
            throw new InvalidOperationException("Only a task that is currently assigned can be reassigned.");

        return task;
    }
}
