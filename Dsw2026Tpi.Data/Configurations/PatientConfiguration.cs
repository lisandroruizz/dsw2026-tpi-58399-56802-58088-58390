using System;
using System.Collections.Generic;
using System.Text;
using Dsw2026Tpi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders; 


namespace Dsw2026Tpi.Data.Configurations; 

internal class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Dni).IsRequired();
        builder.Property(x => x.Email).IsRequired().HasMaxLength(256);
        builder.Property(x => x.ApplicationUserId).IsRequired().HasMaxLength(450);
        builder.Property(x => x.Name).HasMaxLength(100);
        builder.Property(x => x.Phone).HasMaxLength(30);
        builder.Property(x => x.Deleted).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => x.Dni).IsUnique();
        builder.HasIndex(x => x.Email);
        builder.HasIndex(x => x.ApplicationUserId).IsUnique();
        builder.HasQueryFilter(x => !x.Deleted); 
    }
    

}
