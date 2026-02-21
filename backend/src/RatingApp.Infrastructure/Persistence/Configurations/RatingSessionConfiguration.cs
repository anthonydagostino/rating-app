using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RatingApp.Domain.Entities;

namespace RatingApp.Infrastructure.Persistence.Configurations;

public class RatingSessionConfiguration : IEntityTypeConfiguration<RatingSession>
{
    public void Configure(EntityTypeBuilder<RatingSession> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedOnAdd();
        builder.Property(s => s.Title).HasMaxLength(200);
        builder.Property(s => s.Status).IsRequired();
        builder.Property(s => s.CreatedAt).HasDefaultValueSql("NOW()");

        builder.HasOne(s => s.Candidate)
               .WithMany()
               .HasForeignKey(s => s.CandidateId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Creator)
               .WithMany()
               .HasForeignKey(s => s.CreatorId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}