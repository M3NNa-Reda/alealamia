using alealamia.Application.DTOs.Support;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;
using alealamia.Domain.Enums;

namespace alealamia.Application.Services
{
    public class SupportMessageService : ISupportMessageService
    {
        private readonly ISupportMessageRepository _messageRepository;
        private readonly ISupportTicketRepository _ticketRepository;
        private readonly IUnitOfWork _unitOfWork;

        public SupportMessageService(
            ISupportMessageRepository messageRepository,
            ISupportTicketRepository ticketRepository,
            IUnitOfWork unitOfWork)
        {
            _messageRepository = messageRepository;
            _ticketRepository = ticketRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SupportMessageDto> SendAsync(CreateSupportMessageDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SenderUserId))
                throw new ArgumentException("SenderUserId is required.");
            if (string.IsNullOrWhiteSpace(dto.MessageText))
                throw new ArgumentException("The message text is required.");

            var ticket = await _ticketRepository.GetByIdAsync(dto.SupportTicketId)
                ?? throw new KeyNotFoundException($"Support ticket {dto.SupportTicketId} was not found.");

            if (ticket.Status == SupportTicketStatus.Closed)
                throw new InvalidOperationException("Messages cannot be sent to a closed ticket.");

            var message = new SupportMessage
            {
                SupportTicketId = dto.SupportTicketId,
                SenderUserId = dto.SenderUserId,
                MessageText = dto.MessageText.Trim(),
                SentAt = DateTime.UtcNow
            };

            foreach (var fileId in dto.FileIds.Distinct())
            {
                message.SupportMessageFiles.Add(new SupportMessageFile { FileId = fileId });
            }

            await _messageRepository.AddAsync(message);
            await _unitOfWork.CompleteAsync();
            return SupportMapping.ToDto(message);
        }

        public async Task<IEnumerable<SupportMessageDto>> GetByTicketAsync(int supportTicketId)
        {
            var messages = await _messageRepository.GetByTicketIdAsync(supportTicketId);
            return messages.Select(SupportMapping.ToDto);
        }
    }

    internal static class SupportMapping
    {
        public static SupportMessageDto ToDto(SupportMessage m) => new()
        {
            Id = m.Id,
            SupportTicketId = m.SupportTicketId,
            SenderUserId = m.SenderUserId,
            MessageText = m.MessageText,
            SentAt = m.SentAt,
            Files = m.SupportMessageFiles
                .Select(f => new SupportMessageFileDto
                {
                    SupportMessageId = f.SupportMessageId,
                    FileId = f.FileId
                })
                .ToList()
        };
    }
}