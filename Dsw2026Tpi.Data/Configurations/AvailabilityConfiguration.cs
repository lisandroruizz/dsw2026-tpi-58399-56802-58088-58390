using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Dsw2026Tpi.Data.Configurations
{
    public class AvailabilityConfiguration : IEntityTypeConfiguration<Availability>
    {
        public void Configure(EntityTypeBuilder<Availability> builder)
        {
            builder.ToTable("Availabilities");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id).ValueGeneratedNever();
            builder.Property(x => x.DoctorId).IsRequired();
            builder.Property(x => x.Date).HasColumnType("date").IsRequired();
            builder.Property(x => x.StartTime).HasColumnType("time(0)").IsRequired();
            builder.Property(x => x.EndTime).HasColumnType("time(0)").IsRequired();
            builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired();
            builder.Property(x => x.Deleted).IsRequired().HasDefaultValue(false);
            
            builder.HasIndex(x => new { x.DoctorId, x.Date, x.Status });
            builder.HasQueryFilter(x => !x.Deleted);

            builder.HasOne(x => x.Doctor)
                .WithMany(x => x.Availabilities)
                .HasForeignKey(x => x.DoctorId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
