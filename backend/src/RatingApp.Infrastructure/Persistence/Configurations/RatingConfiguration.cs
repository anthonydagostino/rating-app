using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RatingApp.Domain.Entities;

namespace RatingApp.Infrastructure.Persistence.Configurations;

public class RatingConfiguration : IEntityTypeConfiguration<Rating>
{
    public void Configure(EntityTypeBuilder<Rating> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();
        builder.Property(r => r.Score).IsRequired();
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(r => r.UpdatedAt).HasDefaultValueSql("NOW()");

        builder.HasIndex(r => new { r.RaterUserId, r.RatedUserId }).IsUnique();

        builder.HasOne(r => r.Rater)
               .WithMany(u => u.RatingsGiven)
               .HasForeignKey(r => r.RaterUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Rated)
               .WithMany(u => u.RatingsReceived)
               .HasForeignKey(r => r.RatedUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
