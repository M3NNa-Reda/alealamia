using alealamia.Application.DTOs.Common;
using alealamia.Application.DTOs.Track;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;

namespace alealamia.Application.Services
{
    public class TrackService : ITrackService
    {
        private readonly IUnitOfWork _unitOfWork;

        public TrackService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<TrackDto>> GetActiveTracksAsync()
        {
            var tracks = await _unitOfWork.Repository<Track>()
                .FindByConditionAsync(x => x.IsActive);

            return tracks.Select(MapToDto);
        }

        public async Task<IEnumerable<TrackDto>> GetAllTracksForAdminAsync()
        {
            var tracks = await _unitOfWork.Repository<Track>()
                .GetAllAsync();

            return tracks.Select(MapToDto);
        }

        public async Task<TrackDto?> GetTrackByIdAsync(int id)
        {
            var track = await _unitOfWork.Repository<Track>()
                .GetByIdAsync(id);

            return track == null ? null : MapToDto(track);
        }

        public async Task<ResultDto> CreateTrackAsync(CreateTrackDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Failed("Track name is required.");

            var name = dto.Name.Trim();

            var existingTrack = await _unitOfWork.Repository<Track>()
                .FindByConditionAsync(x => x.Name.ToLower() == name.ToLower());

            if (existingTrack.Any())
                return Failed("Track already exists.");

            var track = new Track
            {
                Name = name,
                Description = dto.Description?.Trim(),
                IsActive = true
            };

            await _unitOfWork.Repository<Track>().AddAsync(track);
            await _unitOfWork.CompleteAsync();

            return new ResultDto { Succeeded = true };
        }

        public async Task<ResultDto> UpdateTrackAsync(int id, UpdateTrackDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name))
                return Failed("Track name is required.");

            var repository = _unitOfWork.Repository<Track>();

            var track = await repository.GetByIdAsync(id);

            if (track == null)
                return Failed("Track not found.");

            var name = dto.Name.Trim();

            var existingTrack = await repository.FindByConditionAsync(
                x => x.Id != id && x.Name.ToLower() == name.ToLower());

            if (existingTrack.Any())
                return Failed("Another track already has this name.");

            track.Name = name;
            track.Description = dto.Description?.Trim();

            repository.Update(track);
            await _unitOfWork.CompleteAsync();

            return new ResultDto { Succeeded = true };
        }

        public async Task<ResultDto> DeactivateTrackAsync(int id)
        {
            var repository = _unitOfWork.Repository<Track>();

            var track = await repository.GetByIdAsync(id);

            if (track == null)
                return Failed("Track not found.");

            if (!track.IsActive)
                return Failed("Track is already inactive.");

            track.IsActive = false;

            repository.Update(track);
            await _unitOfWork.CompleteAsync();

            return new ResultDto { Succeeded = true };
        }
        public async Task<ResultDto> ActivateTrackAsync(int id)
        {
            var repository = _unitOfWork.Repository<Track>();

            var track = await repository.GetByIdAsync(id);

            if (track == null)
                return Failed("Track not found.");

            if (track.IsActive)
                return Failed("Track is already active.");

            track.IsActive = true;

            repository.Update(track);
            await _unitOfWork.CompleteAsync();

            return new ResultDto { Succeeded = true };
        }

        private static TrackDto MapToDto(Track track)
        {
            return new TrackDto
            {
                Id = track.Id,
                Name = track.Name,
                Description = track.Description,
                IsActive = track.IsActive
            };
        }

        private static ResultDto Failed(string error)
        {
            return new ResultDto
            {
                Succeeded = false,
                Errors = new List<string> { error }
            };
        }

    }
}