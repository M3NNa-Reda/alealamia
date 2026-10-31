using alealamia.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace alealamia.Infrastructure.Configurations
{
    internal class TaskSubmissionConfiguration : IEntityTypeConfiguration<TaskSubmission>
    {
        public void Configure(EntityTypeBuilder<TaskSubmission> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.SubmissionNotes)
                    .HasMaxLength(2000);

            builder.Property(t => t.Status)
                    .IsRequired();

            builder.HasOne<Domain.Entities.Task>()
                    .WithMany()
                    .HasForeignKey(t => t.TaskId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<FreelancerProfile>()
                    .WithMany()
                    .HasForeignKey(t => t.FreelancerProfileId)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
