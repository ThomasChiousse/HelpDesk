using HelpDesk.Domain;

namespace HelpDesk.Application.Repositories
{
    public interface ITicketRepository
    {
        Task<Ticket?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Ticket?> GetByIdWithAssigneeAsync(int id, CancellationToken cancellationToken = default);
        Task<Ticket?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<(IReadOnlyCollection<Ticket> Items, int TotalCount)> GetPagedAsync(TicketStatus? status, TicketPriority? priority, int? assignedUserId, bool? hasAssignee, string? search,
                                                                                int page, int pageSize, CancellationToken cancellationToken = default);
        Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
