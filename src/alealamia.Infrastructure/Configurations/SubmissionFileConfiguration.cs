using alealamia.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Infrastructure.Configurations
{
    internal class SubmissionFileConfiguration : IEntityTypeConfiguration<SubmissionFile>
    {
        public void Configure(EntityTypeBuilder<SubmissionFile> builder)
        {
            builder.HasKey(t => new { t.TaskSubmissionId, t.FileId });

            builder.Property(t => t.UploadedAt)
                .IsRequired();

            builder.HasOne<TaskSubmission>()
                .WithMany()
                .HasForeignKey(t => t.TaskSubmissionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne<Domain.Entities.File>()
                .WithMany()
                .HasForeignKey(t => t.FileId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
