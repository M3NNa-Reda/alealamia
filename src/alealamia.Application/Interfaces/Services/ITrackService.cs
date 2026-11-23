using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Track;

namespace alealamia.Application.Interfaces.Services
{
    public interface ITrackService
    {
        Task<IEnumerable<TrackDto>> GetActiveTracksAsync();
        Task<IEnumerable<TrackDto>> GetAllTracksForAdminAsync();
        Task<TrackDto?> GetTrackByIdAsync(int id);
        Task<ResultDto> CreateTrackAsync(CreateTrackDto dto);
        Task<ResultDto> UpdateTrackAsync(int id, UpdateTrackDto dto);
        Task<ResultDto> DeactivateTrackAsync(int id);
        Task<ResultDto> ActivateTrackAsync(int id);
    }
}