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
        builder.Property(x => x.AvailabilitySlotId).IsRequired();
        builder.Property(x => x.PatientId).IsRequired(); 
        builder.Property(x => x.Reason).IsRequired().HasMaxLength(300);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(20).IsRequired(); 
        builder.Property(x => x.CancelledAt).HasColumnType("datetime2");
        builder.Property(appointment => appointment.AttendedAt).HasColumnType("datetime2");
        builder.Property(x => x.Deleted).IsRequired().HasDefaultValue(false);
        builder.HasIndex(x => x.AvailabilitySlotId)
            .IsUnique()
            .HasDatabaseName("UX_Appointments_ActiveAvailabilitySlot")
            .HasFilter("[Deleted] = 0 AND [Status] <> 'Cancelled'");

        builder.HasIndex(appointment => new { appointment.PatientId, appointment.Status });
        builder.HasQueryFilter(appointment => !appointment.Deleted); 

        builder.HasOne(appointment => appointment.AvailabilitySlot)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.AvailabilitySlotId)
            .OnDelete(DeleteBehavior.Restrict); 
        
        builder.HasOne(x => x.Patient)
            .WithMany(x => x.Appointments)
            .HasForeignKey(x => x.PatientId)
            .OnDelete(DeleteBehavior.Restrict); 
    } 
}