using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Dsw2026Tpi.Data.Configurations;

public class SpecialityConfiguration : IEntityTypeConfiguration<Speciality>
{
    public void Configure(EntityTypeBuilder<Speciality> builder)
    {
        builder.ToTable("Specialities");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Name).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Description).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Deleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(speciality => speciality.Name).IsUnique().HasDatabaseName("UX_Specialities_ActiveName")
           .HasFilter("[Deleted] = 0");
        builder.HasQueryFilter(x => !x.Deleted); 

            

    }
}
