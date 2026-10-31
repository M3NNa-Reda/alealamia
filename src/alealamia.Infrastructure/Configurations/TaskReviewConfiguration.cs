using alealamia.Domain.Entities;
using alealamia.Infrastructure.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Infrastructure.Configurations
{
    internal class TaskReviewConfiguration : IEntityTypeConfiguration<TaskReview>
    {
        public void Configure(EntityTypeBuilder<TaskReview> builder)
        {
            builder.HasKey(t => t.Id);

            builder.Property(t => t.Decision)
                   .IsRequired();

            builder.Property(t => t.Feedback)
                   .HasMaxLength(2000);

            builder.Property(t => t.ReviewedAt)
                   .IsRequired();

            builder.HasOne<TaskSubmission>()
                    .WithMany()
                    .HasForeignKey(t => t.TaskSubmissionId)
                    .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<ApplicationUser>()
                    .WithMany()
                    .HasForeignKey(t => t.ReviewedByUserId)
                    .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
