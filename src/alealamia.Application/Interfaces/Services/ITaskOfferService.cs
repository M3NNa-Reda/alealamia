using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.TaskOffers;

namespace alealamia.Application.Interfaces.Services;

public interface ITaskOfferService
{
    Task<IEnumerable<TaskOfferDto>> SendOffersAsync(int taskId);
    Task<TaskOfferDto> CreateAsync(CreateTaskOfferDto dto);
    Task<TaskOfferDetailsDto?> GetDetailsAsync(int id);
    Task<PagedResultDto<TaskOfferDetailsDto>> SearchAsync(TaskOfferFilterDto filter);
    Task<TaskOfferResponseDto> RespondAsync(int offerId, RespondToTaskOfferDto dto);
}
