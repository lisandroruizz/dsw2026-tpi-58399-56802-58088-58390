using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment> {
    public void Configure(EntityTypeBuilder<Appointment> builder)
    { 
        builder.ToTable("Appointments");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.AvailabilityId).IsRequired();
        builder.Property(x => x.PatientId).IsRequired(); 
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(500);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired(); 
        builder.Property(x => x.CancelledAt).HasColumnType("datetime2"); 
        builder.Property(x => x.Deleted).IsRequired().HasDefaultValue(false);
        builder.HasIndex(x => x.AvailabilityId)
            .IsUnique()
            .HasDatabaseName("UX_Appointments_ActiveAvailability")
            .HasFilter("[Deleted] = 0 AND [Status] <> 'Cancelled'");

        builder.HasIndex(x => new { x.PatientId, x.Status });
        builder.HasQueryFilter(x => !x.Deleted); 

        builder.HasOne(x => x.Availability)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.AvailabilityId)
            .OnDelete(DeleteBehavior.Restrict); 
        
        builder.HasOne(x => x.Patient)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict); 
    } 
}