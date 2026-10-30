using alealamia.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace alealamia.Infrastructure.Configurations
{
    internal class ClientProfileConfiguration : IEntityTypeConfiguration<ClientProfile>
    {
        public void Configure(EntityTypeBuilder<ClientProfile> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.BusinessType).HasMaxLength(200).IsRequired(false);
            builder.Property(x => x.BusinessName).HasMaxLength(200).IsRequired(false);
            builder.Property(x => x.WebsiteUrl).HasMaxLength(500).IsRequired(false);
            builder.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
        }
    }
}
