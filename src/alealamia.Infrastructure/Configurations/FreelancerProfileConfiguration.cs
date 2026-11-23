using alealamia.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Infrastructure.Configurations
{
    internal class FreelancerProfileConfiguration : IEntityTypeConfiguration<FreelancerProfile>
    {
        public void Configure(EntityTypeBuilder<FreelancerProfile> builder)
        {
            builder.Property(x => x.ApprovalStatus)
                .IsRequired()
                .HasConversion<int>();
            builder.HasOne(x => x.Track)
                .WithMany(x => x.FreelancerProfiles)
                .HasForeignKey(x => x.TrackId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.Property(x => x.About)
                .IsRequired()
                .HasMaxLength(2000)
                .IsRequired(false);
            builder.Property(x => x.ProfileImageUrl)
                .HasMaxLength(500)
                .IsRequired(false);
            builder.Property(x => x.IsAvailable)
                .IsRequired();
            builder.Property(x => x.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(200);
        }
    }
}
