using HelpDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Infrastructure.Persistence.Configurations
{
    public class CommentConfiguration : IEntityTypeConfiguration<Comment>
    {
        public void Configure(EntityTypeBuilder<Comment> builder)
        {
            builder.HasKey(comment => comment.Id);
            builder.Property(comment => comment.Content).IsRequired().HasMaxLength(4000);
            builder.Property(comment => comment.CreationDate);
            builder.HasOne(comment => comment.Author).WithMany().HasForeignKey("AuthorId").IsRequired().OnDelete(DeleteBehavior.Restrict);
        }
    }
}
