using alealamia.Application.DTOs.Meetings;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;

namespace alealamia.Application.Services
{
    public class MeetingService : IMeetingService
    {
        private readonly IMeetingRepository _meetingRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MeetingService(
            IMeetingRepository meetingRepository,
            IProjectRepository projectRepository,
            IUnitOfWork unitOfWork)
        {
            _meetingRepository = meetingRepository;
            _projectRepository = projectRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<MeetingDto> CreateAsync(CreateMeetingDto dto)
        {
            var project = await _projectRepository.GetByIdAsync(dto.ProjectId)
                ?? throw new KeyNotFoundException($"Project {dto.ProjectId} was not found.");

            if (project.ClientProfileId != dto.ClientProfileId)
                throw new InvalidOperationException("The project does not belong to this client.");

            if (project.Status == ProjectStatus.Completed || project.Status == ProjectStatus.Cancelled)
                throw new InvalidOperationException("Meetings cannot be requested for a closed project.");

            if (dto.ProposedAt <= DateTime.UtcNow)
                throw new ArgumentException("The proposed time must be in the future.");

            var meeting = new Meeting
            {
                ProjectId = dto.ProjectId,
                ClientProfileId = dto.ClientProfileId,
                ProposedAt = dto.ProposedAt,
                Status = MeetingStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };

            await _meetingRepository.AddAsync(meeting);
            await _unitOfWork.CompleteAsync();
            return MapToDto(meeting);
        }

        public async Task<MeetingDto> ReviewAsync(ReviewMeetingDto dto)
        {
            if (dto.Status != MeetingStatus.Accepted && dto.Status != MeetingStatus.Rejected)
                throw new ArgumentException("A meeting can only be accepted or rejected.");

            if (string.IsNullOrWhiteSpace(dto.ReviewedByUserId))
                throw new ArgumentException("ReviewedByUserId is required.");

            var meeting = await _meetingRepository.GetByIdAsync(dto.MeetingId)
                ?? throw new KeyNotFoundException($"Meeting {dto.MeetingId} was not found.");

            if (meeting.Status != MeetingStatus.Pending)
                throw new InvalidOperationException("This meeting has already been reviewed.");

            meeting.Status = dto.Status;
            meeting.AdminNotes = dto.AdminNotes;
            meeting.ReviewedByUserId = dto.ReviewedByUserId;
            meeting.ReviewedAt = DateTime.UtcNow;

            _meetingRepository.Update(meeting);
            await _unitOfWork.CompleteAsync();
            return MapToDto(meeting);
        }

        public async Task<MeetingDto?> GetByIdAsync(int id)
        {
            var meeting = await _meetingRepository.GetByIdAsync(id);
            return meeting is null ? null : MapToDto(meeting);
        }

        public async Task<IEnumerable<MeetingDto>> GetByProjectAsync(int projectId)
        {
            var meetings = await _meetingRepository.GetByProjectIdAsync(projectId);
            return meetings.Select(MapToDto);
        }

        public async Task<IEnumerable<MeetingDto>> GetByClientAsync(int clientProfileId)
        {
            var meetings = await _meetingRepository.GetByClientProfileIdAsync(clientProfileId);
            return meetings.Select(MapToDto);
        }

        public async Task<IEnumerable<MeetingDto>> GetByStatusAsync(MeetingStatus status)
        {
            var meetings = await _meetingRepository.GetByStatusAsync(status);
            return meetings.Select(MapToDto);
        }

        private static MeetingDto MapToDto(Meeting m) => new()
        {
            Id = m.Id,
            ProjectId = m.ProjectId,
            ClientProfileId = m.ClientProfileId,
            ProposedAt = m.ProposedAt,
            Status = m.Status,
            AdminNotes = m.AdminNotes,
            ReviewedByUserId = m.ReviewedByUserId,
            ReviewedAt = m.ReviewedAt,
            CreatedAt = m.CreatedAt
        };
    }
}