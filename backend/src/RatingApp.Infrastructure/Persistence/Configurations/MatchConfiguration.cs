using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RatingApp.Domain.Entities;

namespace RatingApp.Infrastructure.Persistence.Configurations;

public class MatchConfiguration : IEntityTypeConfiguration<Match>
{
    public void Configure(EntityTypeBuilder<Match> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();
        builder.Property(m => m.CreatedAt).HasDefaultValueSql("NOW()");

        builder.HasIndex(m => new { m.UserAId, m.UserBId }).IsUnique();

        builder.HasOne(m => m.UserA)
               .WithMany()
               .HasForeignKey(m => m.UserAId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.UserB)
               .WithMany()
               .HasForeignKey(m => m.UserBId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.Chat)
               .WithOne(c => c.Match)
               .HasForeignKey<Chat>(c => c.MatchId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
