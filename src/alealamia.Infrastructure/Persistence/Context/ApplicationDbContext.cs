using alealamia.Domain.Entities;
using alealamia.Infrastructure.Identity.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;


namespace alealamia.Infrastructure.Persistence.Context
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole, string>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<ClientProfile> ClientProfiles { get; set; }
        public DbSet<FreelancerProfile> FreelancerProfiles { get; set; }
        public DbSet<FreelancerBlock> FreelancerBlocks { get; set; }
        public DbSet<Track> Tracks { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<TrackSkill> TrackSkills { get; set; }
        public DbSet<FreelancerSkill> FreelancerSkills { get; set; }
        public DbSet<PortfolioItem> PortfolioItems { get; set; }
        public DbSet<PortfolioItemFile> PortfolioItemFiles { get; set; }
        public DbSet<TaskSubmission> TaskSubmissions { get; set; }
        public DbSet<TaskReview> TaskReviews { get; set; }
        public DbSet<TaskRevision> TaskRevisions { get; set; }
        public DbSet<SubmissionFile> SubmissionFiles { get; set; }
        public DbSet<FreelancerLedgerEntry> FreelancerLedgerEntries { get; set; }
        public DbSet<Domain.Entities.Task> Tasks { get; set; }
        public DbSet<TaskRequiredSkill> TaskRequiredSkills { get; set; }
        public DbSet<TaskOffer> TaskOffers { get; set; }
        public DbSet<TaskStatusHistory> TaskStatusHistory { get; set; }
        public DbSet<TaskReassignment> TaskReassignments { get; set; }
        public DbSet<TaskFile> TaskFiles { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<Project> Projects { get; set; }
        public DbSet<ProjectService> ProjectServices { get; set; }
        public DbSet<ProjectStatusHistory> ProjectStatusHistories { get; set; }
        public DbSet<Meeting> Meetings { get; set; }
        public DbSet<MarketingPackage> MarketingPackages { get; set; }
        public DbSet<MarketingPackageItem> MarketingPackageItems { get; set; }
        public DbSet<ProjectMarketingPackage> ProjectMarketingPackages { get; set; }
        public DbSet<ProjectMarketingPackageItem> ProjectMarketingPackageItems { get; set; }
        public DbSet<Domain.Entities.File> Files { get; set; }
        public DbSet<ProjectFile> ProjectFiles { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<SupportMessage> SupportMessages { get; set; }
        public DbSet<SupportMessageFile> SupportMessageFiles { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<EmailNotificationLog> EmailNotificationLogs { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<ApplicationUser>().ToTable("Users");
            builder.Entity<IdentityRole>().ToTable("Roles");
            builder.Entity<IdentityUserRole<string>>().ToTable("UserRoles");
            builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        }
    }
}
