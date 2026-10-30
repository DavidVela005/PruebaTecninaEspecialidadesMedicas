using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BonusTest.Domain.Entities;

namespace BonusTest.Infrastructure.Persistence.Configurations;

public class BonusConfiguration : IEntityTypeConfiguration<Bonus>
{
    public void Configure(EntityTypeBuilder<Bonus> builder)
    {
        builder.ToTable("Bonus").HasKey(x => x.BonusId);

        builder.Property(x => x.DepartmentCode).HasColumnType("char(5)");
        builder.Property(x => x.ReasonCode).HasColumnType("char(6)");
        builder.Property(x => x.BonusStateCode).HasColumnType("char(4)");
        builder.Property(x => x.BonusValue).HasPrecision(16, 2);
        builder.Property(x => x.TaxRate).HasPrecision(5, 2);
        builder.Property(x => x.GrossAmount).HasPrecision(16, 2);
        builder.Property(x => x.TaxAmount).HasPrecision(16, 2);
        builder.Property(x => x.NetAmount).HasPrecision(16, 2);
        builder.Property(x => x.CreatedBy).HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.ModifiedBy).HasMaxLength(50).IsUnicode(false);
        builder.Property(x => x.CreatedAt).HasColumnType("datetime2(0)");
        builder.Property(x => x.ModifiedAt).HasColumnType("datetime2(0)");

        // La eliminación es lógica: los registros eliminados no aparecen en ninguna consulta.
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Employee).WithMany()
            .HasForeignKey(x => new { x.DepartmentCode, x.EmployeeNumber });
        builder.HasOne(x => x.BonusType).WithMany().HasForeignKey(x => x.BonusTypeId);
        builder.HasOne(x => x.Reason).WithMany().HasForeignKey(x => x.ReasonCode);
        builder.HasOne(x => x.State).WithMany().HasForeignKey(x => x.BonusStateCode);
    }
}
