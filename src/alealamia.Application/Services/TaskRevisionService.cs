using alealamia.Application.DTOs.TaskRevision;
using alealamia.Application.Interfaces.Repositories;
using alealamia.Application.Interfaces.Services;
using alealamia.Domain.Entities;

namespace alealamia.Application.Services
{
    public class TaskRevisionService : ITaskRevisionService
    {
        private readonly IUnitOfWork _unitOfWork;
        public TaskRevisionService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<TaskRevisionDto> CreateRevisionAsync(CreateTaskRevisionDto revision)
        {
            TaskRevision taskRevision = new TaskRevision()
            {
                TaskId = revision.TaskId,
                TaskReviewId = revision.TaskReviewId,
                RevisionNotes = revision.RevisionNotes,
                NewDeadline = revision.NewDeadline,
                CreatedAt = DateTime.UtcNow,
                Status = RevisionStatus.Pending
            };
            await _unitOfWork.TaskRevisionRepository.AddAsync(taskRevision);
            await _unitOfWork.CompleteAsync();

            TaskRevisionDto taskRevisionDto = new TaskRevisionDto
            {
                Id = taskRevision.Id,
                TaskId = taskRevision.TaskId,
                TaskReviewId = taskRevision.TaskReviewId,
                RevisionNotes = taskRevision.RevisionNotes,
                NewDeadline = taskRevision.NewDeadline,
                CreatedAt = taskRevision.CreatedAt,
                Status = taskRevision.Status,
                CompletedAt = taskRevision.CompletedAt
            };
            return taskRevisionDto;
        }
        public async Task<TaskRevisionDto?> CompleteRevisionAsync(int revisionId)
        {
            TaskRevision? taskRevision = await _unitOfWork.TaskRevisionRepository
                .GetByIdAsync(revisionId);

            if (taskRevision == null) return null;

            taskRevision.Status = RevisionStatus.Completed;
            taskRevision.CompletedAt = DateTime.UtcNow;

            _unitOfWork.TaskRevisionRepository.Update(taskRevision);
            await _unitOfWork.CompleteAsync();

            var result = new TaskRevisionDto
            {
                Id = taskRevision.Id,
                TaskId = taskRevision.TaskId,
                TaskReviewId = taskRevision.TaskReviewId,
                RevisionNotes = taskRevision.RevisionNotes,
                NewDeadline = taskRevision.NewDeadline,
                CreatedAt = taskRevision.CreatedAt,
                Status = taskRevision.Status,
                CompletedAt = taskRevision.CompletedAt
            };

            return result;

        }

        public async Task<TaskRevisionDto?> GetByIdAsync(int id)
        {
            TaskRevision? taskRevision = await _unitOfWork.TaskRevisionRepository
                 .GetByIdAsync(id);

            if (taskRevision == null) return null;

            var result = new TaskRevisionDto
            {
                Id = taskRevision.Id,
                TaskId = taskRevision.TaskId,
                TaskReviewId = taskRevision.TaskReviewId,
                RevisionNotes = taskRevision.RevisionNotes,
                NewDeadline = taskRevision.NewDeadline,
                CreatedAt = taskRevision.CreatedAt,
                Status = taskRevision.Status,
                CompletedAt = taskRevision.CompletedAt
            };

            return result;
        }

        public async Task<IEnumerable<TaskRevisionDto>> GetByTaskIdAsync(int taskId)
        {
            IEnumerable<TaskRevision> taskRevisions = await _unitOfWork.TaskRevisionRepository
                .GetByTaskIdAsync(taskId);
            var result = taskRevisions.Select(t => new TaskRevisionDto
            {
                Id = t.Id,
                TaskId = t.TaskId,
                TaskReviewId = t.TaskReviewId,
                RevisionNotes = t.RevisionNotes,
                NewDeadline = t.NewDeadline,
                CreatedAt = t.CreatedAt,
                Status = t.Status,
                CompletedAt = t.CompletedAt
            });


            return result;
        }

        public async Task<IEnumerable<TaskRevisionDto>> GetByTaskReviewIdAsync(int taskReviewId)
        {
            IEnumerable<TaskRevision> taskRevisions = await _unitOfWork.TaskRevisionRepository
               .GetByTaskReviewIdAsync(taskReviewId);
            var result = taskRevisions.Select(t => new TaskRevisionDto
            {
                Id = t.Id,
                TaskId = t.TaskId,
                TaskReviewId = t.TaskReviewId,
                RevisionNotes = t.RevisionNotes,
                NewDeadline = t.NewDeadline,
                CreatedAt = t.CreatedAt,
                Status = t.Status,
                CompletedAt = t.CompletedAt
            });


            return result;
        }
    }
}
