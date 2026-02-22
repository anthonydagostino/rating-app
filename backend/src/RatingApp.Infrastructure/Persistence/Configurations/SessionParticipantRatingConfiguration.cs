using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RatingApp.Domain.Entities;

namespace RatingApp.Infrastructure.Persistence.Configurations;

public class SessionParticipantRatingConfiguration : IEntityTypeConfiguration<SessionParticipantRating>
{
    public void Configure(EntityTypeBuilder<SessionParticipantRating> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedOnAdd();
        builder.Property(r => r.Notes).HasMaxLength(1000);
        builder.Property(r => r.CreatedAt).HasDefaultValueSql("NOW()");
        builder.Property(r => r.UpdatedAt).HasDefaultValueSql("NOW()");

        builder.HasIndex(r => new { r.SessionId, r.RaterUserId }).IsUnique();

        builder.HasOne(r => r.Session)
               .WithMany(s => s.ParticipantRatings)
               .HasForeignKey(r => r.SessionId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.Rater)
               .WithMany()
               .HasForeignKey(r => r.RaterUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}