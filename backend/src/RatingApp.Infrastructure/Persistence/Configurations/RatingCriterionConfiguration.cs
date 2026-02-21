using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RatingApp.Domain.Entities;

namespace RatingApp.Infrastructure.Persistence.Configurations;

public class RatingCriterionConfiguration : IEntityTypeConfiguration<RatingCriterion>
{
    public void Configure(EntityTypeBuilder<RatingCriterion> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.Weight)
            .IsRequired();

        builder.Property(c => c.IsRequired)
            .IsRequired();

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.ToTable("RatingCriteria");
    }
}
