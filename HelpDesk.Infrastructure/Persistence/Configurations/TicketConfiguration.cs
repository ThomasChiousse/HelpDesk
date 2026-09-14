using HelpDesk.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HelpDesk.Infrastructure.Persistence.Configurations
{
    public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
    {
        public void Configure(EntityTypeBuilder<Ticket> builder)
        {
            builder.HasKey(ticket => ticket.Id);
            builder.Property(ticket => ticket.Title).IsRequired().HasMaxLength(200);
            builder.Property(ticket => ticket.Description).HasMaxLength(4000);
            builder.Property(ticket => ticket.Status);
            builder.Property(ticket => ticket.Priority);
            builder.Property(ticket => ticket.CreationDate);
            builder.HasOne(ticket => ticket.AssignedUser).WithMany().HasForeignKey("AssignedUserId").IsRequired(false).OnDelete(DeleteBehavior.SetNull);
            builder.HasMany(ticket => ticket.Comments).WithOne().HasForeignKey("TicketId").IsRequired().OnDelete(DeleteBehavior.Cascade);
        }
    }
}
