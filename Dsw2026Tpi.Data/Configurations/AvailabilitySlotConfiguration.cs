using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class AvailabilitySlotConfiguration
    : IEntityTypeConfiguration<AvailabilitySlot>
{
    public void Configure(
        EntityTypeBuilder<AvailabilitySlot> builder)
    {
        builder.ToTable("AvailabilitySlots");
        builder.HasKey(slot => slot.Id);

        builder.Property(slot => slot.Id)
            .ValueGeneratedNever();

        builder.Property(slot =>
                slot.AvailabilityRuleId);

        builder.Property(slot => slot.DoctorId)
            .IsRequired();

        builder.Property(slot => slot.SlotDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(slot => slot.StartTime)
            .HasColumnType("time(0)")
            .IsRequired();

        builder.Property(slot => slot.EndTime)
            .HasColumnType("time(0)")
            .IsRequired();

        builder.Property(slot => slot.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(slot => slot.Deleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property<byte[]>("RowVersion")
            .IsRowVersion();

        builder.HasIndex(slot => new
            {
                slot.DoctorId,
                slot.SlotDate,
                slot.StartTime
            })
            .IsUnique()
            .HasDatabaseName(
                "UX_AvailabilitySlots_Doctor_Date_StartTime")
            .HasFilter("[Deleted] = 0");

        builder.HasIndex(slot => new
            {
                slot.DoctorId,
                slot.SlotDate,
                slot.Status
            });

        builder.HasQueryFilter(
            slot => !slot.Deleted);

        builder.HasOne(slot =>
                slot.AvailabilityRule)
            .WithMany(rule => rule.Slots)
            .HasForeignKey(slot =>
                slot.AvailabilityRuleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(slot => slot.Doctor)
            .WithMany(doctor =>
                doctor.AvailabilitySlots)
            .HasForeignKey(slot => slot.DoctorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
