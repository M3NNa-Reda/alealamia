using alealamia.Application.DTOs.Support;
using alealamia.Domain.Enums;

namespace alealamia.Application.Interfaces.Services
{
    public interface ISupportTicketService
    {
        Task<SupportTicketDto> CreateAsync(CreateSupportTicketDto dto);
        Task<SupportTicketDetailsDto?> GetDetailsAsync(int id);
        Task<IEnumerable<SupportTicketDto>> GetByUserAsync(string userId);
        Task<IEnumerable<SupportTicketDto>> GetByStatusAsync(SupportTicketStatus status);
        Task<SupportTicketDto> UpdateStatusAsync(int id, UpdateSupportTicketStatusDto dto);
    }
}