using alealamia.Application.DTOs.Support;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;

namespace alealamia.Application.Services
{
    public class SupportTicketService : ISupportTicketService
    {
        private readonly ISupportTicketRepository _ticketRepository;
        private readonly ISupportMessageRepository _messageRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SupportTicketService(
            ISupportTicketRepository ticketRepository,
            ISupportMessageRepository messageRepository,
            IUnitOfWork unitOfWork)
        {
            _ticketRepository = ticketRepository;
            _messageRepository = messageRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SupportTicketDto> CreateAsync(CreateSupportTicketDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.CreatedByUserId))
                throw new ArgumentException("CreatedByUserId is required.");
            if (string.IsNullOrWhiteSpace(dto.Subject) || dto.Subject.Trim().Length > 200)
                throw new ArgumentException("Subject is required and must not exceed 200 characters.");
            if (string.IsNullOrWhiteSpace(dto.InitialMessage))
                throw new ArgumentException("The first message is required.");

            var ticket = new SupportTicket
            {
                CreatedByUserId = dto.CreatedByUserId,
                Subject = dto.Subject.Trim(),
                Status = SupportTicketStatus.Open,
                Priority = dto.Priority,
                CreatedAt = DateTime.UtcNow
            };

            ticket.SupportMessages.Add(new SupportMessage
            {
                SenderUserId = dto.CreatedByUserId,
                MessageText = dto.InitialMessage.Trim(),
                SentAt = DateTime.UtcNow
            });

            await _ticketRepository.AddAsync(ticket);
            await _unitOfWork.CompleteAsync();
            return MapToDto(ticket);
        }

        public async Task<SupportTicketDetailsDto?> GetDetailsAsync(int id)
        {
            var ticket = await _ticketRepository.GetByIdAsync(id);
            if (ticket is null)
                return null;

            var messages = await _messageRepository.GetByTicketIdAsync(id);

            return new SupportTicketDetailsDto
            {
                Id = ticket.Id,
                CreatedByUserId = ticket.CreatedByUserId,
                Subject = ticket.Subject,
                Status = ticket.Status,
                Priority = ticket.Priority,
                CreatedAt = ticket.CreatedAt,
                ClosedAt = ticket.ClosedAt,
                Messages = messages.Select(SupportMapping.ToDto).ToList()
            };
        }

        public async Task<IEnumerable<SupportTicketDto>> GetByUserAsync(string userId)
        {
            var tickets = await _ticketRepository.GetByUserIdAsync(userId);
            return tickets.Select(MapToDto);
        }

        public async Task<IEnumerable<SupportTicketDto>> GetByStatusAsync(SupportTicketStatus status)
        {
            var tickets = await _ticketRepository.GetByStatusAsync(status);
            return tickets.Select(MapToDto);
        }

        public async Task<SupportTicketDto> UpdateStatusAsync(int id, UpdateSupportTicketStatusDto dto)
        {
            var ticket = await _ticketRepository.GetByIdAsync(id)
                ?? throw new KeyNotFoundException($"Support ticket {id} was not found.");

            if (ticket.Status == SupportTicketStatus.Closed)
                throw new InvalidOperationException("A closed ticket cannot be modified.");

            ticket.Status = dto.Status;

            if (dto.Priority.HasValue)
                ticket.Priority = dto.Priority.Value;

            if (dto.Status == SupportTicketStatus.Closed)
                ticket.ClosedAt = DateTime.UtcNow;

            _ticketRepository.Update(ticket);
            await _unitOfWork.CompleteAsync();
            return MapToDto(ticket);
        }

        private static SupportTicketDto MapToDto(SupportTicket t) => new()
        {
            Id = t.Id,
            CreatedByUserId = t.CreatedByUserId,
            Subject = t.Subject,
            Status = t.Status,
            Priority = t.Priority,
            CreatedAt = t.CreatedAt,
            ClosedAt = t.ClosedAt
        };
    }
}