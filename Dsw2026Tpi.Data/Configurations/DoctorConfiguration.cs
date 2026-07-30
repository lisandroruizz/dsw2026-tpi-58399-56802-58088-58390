using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.LicenseNumber).IsRequired().HasMaxLength(100);
        builder.Property(x => x.SpecialityId).IsRequired();
        builder.Property(x => x.Deleted).IsRequired().HasDefaultValue(false);
        builder.HasIndex(x => x.Name);
        builder.HasQueryFilter(x => !x.Deleted);
        builder.HasOne(x => x.Speciality)
        .WithMany(x => x.Doctors)
        .HasForeignKey(x => x.SpecialityId)
        .OnDelete(DeleteBehavior.Restrict);

    }
}
