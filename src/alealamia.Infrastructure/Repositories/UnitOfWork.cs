using alealamia.Application.Interfaces.Repositories;
using alealamia.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore.Storage;

namespace alealamia.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction? _transaction;
        public ITaskSubmissionRepository TaskSubmissionRepository { get; private set; }
        public ITaskReviewRepository TaskReviewRepository { get; private set; }
        public ITaskRevisionRepository TaskRevisionRepository { get; private set; }
        public ISubmissionFileRepository SubmissionFileRepository { get; private set; }
        public IFreelancerLedgerEntryRepository FreelancerLedgerEntryRepository { get; private set; }
        public IFileRepository FileRepository { get; private set; }
        public ISupportTicketRepository SupportTicketRepository { get; private set; }
        public ISupportMessageRepository SupportMessageRepository { get; private set; }
        public ISupportMessageFileRepository SupportMessageFileRepository { get; private set; }

        public UnitOfWork(ApplicationDbContext context
            , ITaskSubmissionRepository taskSubmissionRepository
            , ITaskReviewRepository taskReviewRepository
            , ITaskRevisionRepository taskRevisionRepository
            , ISubmissionFileRepository submissionFileRepository
            , IFreelancerLedgerEntryRepository freelancerLedgerEntryRepository
            , IFileRepository fileRepository
            , ISupportTicketRepository supportTicketRepository
            , ISupportMessageRepository supportMessageRepository
            , ISupportMessageFileRepository supportMessageFileRepository
            )
        {
            _context = context;
            TaskSubmissionRepository = taskSubmissionRepository;
            TaskReviewRepository = taskReviewRepository;
            TaskRevisionRepository = taskRevisionRepository;
            FreelancerLedgerEntryRepository = freelancerLedgerEntryRepository;
            FileRepository = fileRepository;
            SubmissionFileRepository = submissionFileRepository;
            SupportTicketRepository = supportTicketRepository;
            SupportMessageRepository = supportMessageRepository;
            SupportMessageFileRepository = supportMessageFileRepository;
        }

        public async Task<int> CompleteAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            if (_transaction != null)
                throw new InvalidOperationException("A transaction is already in progress.");

            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            if (_transaction == null)
                throw new InvalidOperationException("No active transaction.");

            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction == null)
                return;

            try
            {
                await _transaction.RollbackAsync();
            }
            finally
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context.Dispose();
        }
    }
}