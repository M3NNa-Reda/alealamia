namespace alealamia.Application.Interfaces.Repositories
{
    public interface IUnitOfWork : IDisposable
    {
        ITaskSubmissionRepository TaskSubmissionRepository { get; }
        ITaskReviewRepository TaskReviewRepository { get; }
        ITaskRevisionRepository TaskRevisionRepository { get; }
        ISubmissionFileRepository SubmissionFileRepository { get; }
        IFreelancerLedgerEntryRepository FreelancerLedgerEntryRepository { get; }
        IFileRepository FileRepository { get; }
        ISupportTicketRepository SupportTicketRepository { get; }
        ISupportMessageRepository SupportMessageRepository { get; }
        ISupportMessageFileRepository SupportMessageFileRepository { get; }
        IFreelancerProfileRepository FreelancerProfiles { get; }
        
        IGenericRepository<T> Repository<T>() where T : class;
        Task<int> CompleteAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}