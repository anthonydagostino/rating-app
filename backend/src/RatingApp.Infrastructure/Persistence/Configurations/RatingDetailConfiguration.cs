using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RatingApp.Domain.Entities;

namespace RatingApp.Infrastructure.Persistence.Configurations;

public class RatingDetailConfiguration : IEntityTypeConfiguration<RatingDetail>
{
    public void Configure(EntityTypeBuilder<RatingDetail> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).ValueGeneratedOnAdd();

        builder.Property(d => d.Score).IsRequired();

        builder.HasIndex(d => new { d.RatingId, d.CriterionId }).IsUnique();

        builder.HasOne(d => d.Rating)
            .WithMany(r => r.RatingDetails)
            .HasForeignKey(d => d.RatingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.Criterion)
            .WithMany(c => c.RatingDetails)
            .HasForeignKey(d => d.CriterionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable("RatingDetails");
    }
}