using alealamia.Application.DTOs.TaskSubmission;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;

namespace alealamia.Application.Services
{
    public class TaskSubmissionService : ITaskSubmissionService
    {
        private readonly IUnitOfWork _unitOfWork;   
        public TaskSubmissionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<TaskSubmissionDetailsDto> CreateSubmissionAsync(CreateTaskSubmissionDto submission)
        {
            TaskSubmission taskSubmission = new TaskSubmission()
            {
                 TaskId = submission.TaskId,
                 SubmissionNotes = submission.SubmissionNotes,
                 SubmittedAt = DateTime.UtcNow,
                 Status = SubmissionStatus.Submitted
                //FreelancerProfileId declare in auth
            };

            await _unitOfWork.TaskSubmissionRepository.AddAsync(taskSubmission);
            await _unitOfWork.CompleteAsync();

            TaskSubmissionDetailsDto taskSubmissionDetailsDto = new TaskSubmissionDetailsDto()
            {
                Id = taskSubmission.Id,
                TaskId = taskSubmission.TaskId,
                SubmittedAt = taskSubmission.SubmittedAt,
                Status = taskSubmission.Status,
                SubmissionNotes = taskSubmission.SubmissionNotes,
                FreelancerProfileId = taskSubmission.FreelancerProfileId
            };

            return taskSubmissionDetailsDto;
        }

        public async Task<IEnumerable<TaskSubmissionDto>> GetByFreelancerProfileIdAsync(int freelancerProfileId)
        {
            IEnumerable<TaskSubmission>  taskSubmissions = await _unitOfWork.
                TaskSubmissionRepository.GetByFreelancerProfileIdAsync(freelancerProfileId);

            var result = taskSubmissions.Select(t => new TaskSubmissionDto
            {
                Id = t.Id,
                TaskId = t.TaskId,
                Status = t.Status,
                SubmittedAt = t.SubmittedAt
            });
            return result;
        }

        public async Task<TaskSubmissionDetailsDto?> GetByIdAsync(int id)
        {
            TaskSubmission? taskSubmission = await _unitOfWork.
                TaskSubmissionRepository.GetByIdAsync(id);
            
            if (taskSubmission == null)
                return null;
            var result = new TaskSubmissionDetailsDto
            {
                Id = taskSubmission.Id,
                TaskId = taskSubmission.TaskId,
                Status = taskSubmission.Status,
                SubmittedAt = taskSubmission.SubmittedAt,
                FreelancerProfileId = taskSubmission.FreelancerProfileId,
                SubmissionNotes = taskSubmission.SubmissionNotes
            };
            return result;
        }

        public async Task<IEnumerable<TaskSubmissionDto>> GetByTaskIdAsync(int taskId)
        {
            IEnumerable<TaskSubmission> taskSubmissions = await _unitOfWork.
               TaskSubmissionRepository.GetByTaskIdAsync(taskId);

            var result = taskSubmissions.Select(t => new TaskSubmissionDto
            {
                Id = t.Id,
                TaskId = t.TaskId,
                Status = t.Status,
                SubmittedAt = t.SubmittedAt
            });
            return result;
        }
    }
}
