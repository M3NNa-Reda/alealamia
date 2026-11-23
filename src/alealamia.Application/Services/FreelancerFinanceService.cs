using alealamia.Application.DTOs.FinanceLedger;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;
using TaskEntity = alealamia.Domain.Entities.Task;

namespace alealamia.Application.Services
{
    public class FreelancerFinanceService : IFreelancerFinanceService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITaskRepository _taskRepository;

        public FreelancerFinanceService(IUnitOfWork unitOfWork, ITaskRepository taskRepository)
        {
            _unitOfWork = unitOfWork;
            _taskRepository = taskRepository;
        }

        public async Task<FreelancerLedgerEntryDto> RecordEarningAsync(int taskId, string? createdByUserId)
        {
            var task = await GetTaskAsync(taskId);

            FreelancerLedgerEntry? oldEntry = await GetEntryAsync(taskId, LedgerEntryType.Earning);
            if (oldEntry != null)
                return ToDto(oldEntry);

            FreelancerLedgerEntry entry = new FreelancerLedgerEntry
            {
                FreelancerProfileId = task.AssignedFreelancerId!.Value,
                TaskId = taskId,
                EntryType = LedgerEntryType.Earning,
                Amount = task.RewardAmount,
                Reason = "Task Reward",
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            };

            await _unitOfWork.FreelancerLedgerEntryRepository.AddAsync(entry);
            await _unitOfWork.CompleteAsync();

            return ToDto(entry);
        }

        public async Task<FreelancerLedgerEntryDto> ApplyPlatformDeductionAsync(int taskId, string reason, string? createdByUserId)
        {
            if (string.IsNullOrWhiteSpace(reason))
                throw new ArgumentException("Reason is required.");

            if (reason.Length > 500)
                throw new ArgumentException("Reason cannot be longer than 500 characters.");

            var task = await GetTaskAsync(taskId);

            FreelancerLedgerEntry? oldEntry = await GetEntryAsync(taskId, LedgerEntryType.Deduction);
            if (oldEntry != null)
                return ToDto(oldEntry);

            FreelancerLedgerEntry entry = new FreelancerLedgerEntry
            {
                FreelancerProfileId = task.AssignedFreelancerId!.Value,
                TaskId = taskId,
                EntryType = LedgerEntryType.Deduction,
                Amount = Math.Round(task.RewardAmount * 0.20m, 2),
                Reason = reason.Trim(),
                CreatedAt = DateTime.UtcNow,
                CreatedByUserId = createdByUserId
            };

            await _unitOfWork.FreelancerLedgerEntryRepository.AddAsync(entry);
            await _unitOfWork.CompleteAsync();

            return ToDto(entry);
        }

        public async Task<IEnumerable<FreelancerLedgerEntryDto>> GetLedgerByFreelancerProfileIdAsync(int freelancerProfileId)
        {
            IEnumerable<FreelancerLedgerEntry> entries = await _unitOfWork.FreelancerLedgerEntryRepository
                .GetByFreelancerProfileIdAsync(freelancerProfileId);

            return entries.Select(ToDto).ToList();
        }

        public async Task<IEnumerable<FreelancerLedgerEntryDto>> GetLedgerByTaskIdAsync(int taskId)
        {
            IEnumerable<FreelancerLedgerEntry> entries = await _unitOfWork.FreelancerLedgerEntryRepository
                .GetByTaskIdAsync(taskId);

            return entries.Select(ToDto).ToList();
        }

        public async Task<FreelancerFinanceSummaryDto> GetFinanceSummaryAsync(int freelancerProfileId)
        {
            IEnumerable<FreelancerLedgerEntry> entries = await _unitOfWork.FreelancerLedgerEntryRepository
                .GetByFreelancerProfileIdAsync(freelancerProfileId);

            var earnings = entries.Where(e => e.EntryType == LedgerEntryType.Earning).Sum(e => e.Amount);
            var deductions = entries.Where(e => e.EntryType == LedgerEntryType.Deduction).Sum(e => e.Amount);

            return new FreelancerFinanceSummaryDto
            {
                FreelancerProfileId = freelancerProfileId,
                TotalEarnings = earnings,
                TotalDeductions = deductions,
                NetBalance = earnings - deductions
            };
        }

        // عشان اجيب التاسك المعينه الي هنفذ عليها العمليات

        private async Task<TaskEntity> GetTaskAsync(int taskId)
        {
            var task = await _taskRepository.GetReadOnlyByIdAsync(taskId);

            if (task == null)
                throw new KeyNotFoundException("Task not found.");

            if (task.AssignedFreelancerId == null)
                throw new InvalidOperationException("Task is not assigned to a freelancer.");

            if (task.RewardAmount <= 0)
                throw new InvalidOperationException("Task reward must be greater than zero.");

            return task;
        }
        // عشان نرجع هنعمل خصم ولا اضافه للفلوس بعج م نتاكد عن طريق الميثود دي انه م معموله خصم او مكسب قبل كده
        //بتروح لل داتا بيز عشان تشوف التاسكايه دي معمول عليها قبل كده عمليه ولا ايه
        private async Task<FreelancerLedgerEntry?> GetEntryAsync(int taskId, LedgerEntryType type)
        {
            var entries = await _unitOfWork.FreelancerLedgerEntryRepository.GetByTaskIdAsync(taskId);
            return entries.FirstOrDefault(e => e.EntryType == type);
        }
        // بتعمل mapping الي جاي من الداتا بيز تحوله ل dto
        private static FreelancerLedgerEntryDto ToDto(FreelancerLedgerEntry entry)
        {
            return new FreelancerLedgerEntryDto
            {
                Id = entry.Id,
                FreelancerProfileId = entry.FreelancerProfileId,
                TaskId = entry.TaskId,
                EntryType = entry.EntryType,
                Amount = entry.Amount,
                Reason = entry.Reason,
                CreatedAt = entry.CreatedAt,
                CreatedByUserId = entry.CreatedByUserId
            };
        }
    }
}