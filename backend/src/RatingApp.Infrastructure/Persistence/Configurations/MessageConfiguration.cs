using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RatingApp.Domain.Entities;

namespace RatingApp.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedOnAdd();
        builder.Property(m => m.Content).IsRequired().HasMaxLength(2000);
        builder.Property(m => m.CreatedAt).HasDefaultValueSql("NOW()");

        builder.HasOne(m => m.Sender)
               .WithMany(u => u.MessagesSent)
               .HasForeignKey(m => m.SenderUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
