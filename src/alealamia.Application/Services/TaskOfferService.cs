using alealamia.Application.Common;
using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.TaskOffers;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Application.Mappings;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;
using TaskEntity = alealamia.Domain.Entities.Task;
using TaskStatus = alealamia.Domain.Enums.TaskStatus;

namespace alealamia.Application.Services;

public class TaskOfferService : ITaskOfferService
{
    private readonly ITaskOfferRepository _offerRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly ITaskEligibilityService _eligibilityService;
    private readonly ITaskAssignmentService _assignmentService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly TimeProvider _timeProvider;

    public TaskOfferService(
        ITaskOfferRepository offerRepository,
        ITaskRepository taskRepository,
        ITaskEligibilityService eligibilityService,
        ITaskAssignmentService assignmentService,
        IUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _offerRepository = offerRepository;
        _taskRepository = taskRepository;
        _eligibilityService = eligibilityService;
        _assignmentService = assignmentService;
        _unitOfWork = unitOfWork;
        _timeProvider = timeProvider;
    }

    public async Task<IEnumerable<TaskOfferDto>> SendOffersAsync(int taskId)
    {
        var task = await GetPublishedTaskAsync(taskId);

        var eligible = await _eligibilityService.GetEligibleFreelancersAsync(task.Id);
        var alreadyOffered = await _offerRepository.GetOfferedFreelancerIdsAsync(task.Id);
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        var offers = eligible
            .Where(e => !alreadyOffered.Contains(e.FreelancerProfileId))
            .Select(e => new TaskOffer
            {
                TaskId = task.Id,
                FreelancerProfileId = e.FreelancerProfileId,
                Status = TaskOfferStatus.Pending,
                SentAt = now
            })
            .ToList();

        if (offers.Count == 0)
            return Array.Empty<TaskOfferDto>();

        await _offerRepository.AddRangeAsync(offers);
        await _unitOfWork.CompleteAsync();
        return offers.Select(o => o.ToDto()).ToList();
    }

    public async Task<TaskOfferDto> CreateAsync(CreateTaskOfferDto dto)
    {
        var task = await GetPublishedTaskAsync(dto.TaskId);

        var eligibility = await _eligibilityService.CheckAsync(task.Id, dto.FreelancerProfileId);
        if (!eligibility.IsEligible)
            throw new InvalidOperationException(string.Join(" ", eligibility.Reasons));

        var existing = await _offerRepository.GetOfferedFreelancerIdsAsync(task.Id);
        if (existing.Contains(dto.FreelancerProfileId))
            throw new InvalidOperationException("An offer was already sent to this freelancer for the task.");

        var offer = new TaskOffer
        {
            TaskId = task.Id,
            FreelancerProfileId = dto.FreelancerProfileId,
            Status = TaskOfferStatus.Pending,
            SentAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        await _offerRepository.AddAsync(offer);
        await _unitOfWork.CompleteAsync();
        return offer.ToDto();
    }

    public async Task<TaskOfferDetailsDto?> GetDetailsAsync(int id)
    {
        var offer = await _offerRepository.GetDetailsByIdAsync(id);
        return offer?.ToDetailsDto();
    }

    public async Task<PagedResultDto<TaskOfferDetailsDto>> SearchAsync(TaskOfferFilterDto filter)
    {
        var (page, pageSize) = Paging.Normalize(filter.Page, filter.PageSize);
        var (items, total) = await _offerRepository.SearchAsync(
            filter.FreelancerProfileId,
            filter.TaskId,
            filter.Status,
            page,
            pageSize);

        return new PagedResultDto<TaskOfferDetailsDto>
        {
            Items = items.Select(o => o.ToDetailsDto()).ToList(),
            TotalCount = total,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<TaskOfferResponseDto> RespondAsync(int offerId, RespondToTaskOfferDto dto)
    {
        var offer = await _offerRepository.GetDetailsByIdAsync(offerId)
            ?? throw new KeyNotFoundException($"Offer {offerId} was not found.");

        if (offer.FreelancerProfileId != dto.FreelancerProfileId)
            throw new UnauthorizedAccessException("This offer was not sent to the freelancer.");

        if (offer.Status != TaskOfferStatus.Pending)
            throw new InvalidOperationException($"This offer is no longer available ({offer.Status}).");

        if (!dto.Accept)
        {
            var now = _timeProvider.GetUtcNow().UtcDateTime;
            var declined = await _offerRepository.TryDeclineAsync(offerId, now);
            if (!declined)
                throw new InvalidOperationException("This offer is no longer available.");

            var declinedOffer = offer.ToDto();
            declinedOffer.Status = TaskOfferStatus.Declined;
            declinedOffer.RespondedAt = now;
            return new TaskOfferResponseDto { Offer = declinedOffer };
        }

        var assignment = await _assignmentService.AcceptOfferAsync(offerId, dto.FreelancerProfileId);

        var acceptedOffer = offer.ToDto();
        acceptedOffer.Status = TaskOfferStatus.Accepted;
        acceptedOffer.RespondedAt = assignment.AssignedAt;
        return new TaskOfferResponseDto { Offer = acceptedOffer, Assignment = assignment };
    }

    private async Task<TaskEntity> GetPublishedTaskAsync(int taskId)
    {
        var task = await _taskRepository.GetReadOnlyByIdAsync(taskId)
            ?? throw new KeyNotFoundException($"Task {taskId} was not found.");

        if (task.Status != TaskStatus.Published)
            throw new InvalidOperationException("Offers can only be managed for a published task.");

        return task;
    }
}
