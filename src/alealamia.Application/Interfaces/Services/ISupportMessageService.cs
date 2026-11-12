using alealamia.Application.DTOs.Support;

namespace alealamia.Application.Interfaces.Services
{
    public interface ISupportMessageService
    {
        Task<SupportMessageDto> SendAsync(CreateSupportMessageDto dto);
        Task<IEnumerable<SupportMessageDto>> GetByTicketAsync(int supportTicketId);
    }
}