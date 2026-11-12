using alealamia.Application.DTOs.Meetings;
using alealamia.Domain.Enums;

namespace alealamia.Application.Interfaces.Services
{
    public interface IMeetingService
    {
        Task<MeetingDto> CreateAsync(CreateMeetingDto dto);
        Task<MeetingDto> ReviewAsync(ReviewMeetingDto dto);
        Task<MeetingDto?> GetByIdAsync(int id);
        Task<IEnumerable<MeetingDto>> GetByProjectAsync(int projectId);
        Task<IEnumerable<MeetingDto>> GetByClientAsync(int clientProfileId);
        Task<IEnumerable<MeetingDto>> GetByStatusAsync(MeetingStatus status);
    }
}