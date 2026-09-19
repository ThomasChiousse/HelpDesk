using HelpDesk.Application.Common.Pagination;
using HelpDesk.Application.Tickets.Queries;
using HelpDesk.Domain;

namespace HelpDesk.Application.Repositories
{
    public interface ITicketRepository
    {
        Task<Ticket?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Ticket?> GetByIdWithAssigneeAsync(int id, CancellationToken cancellationToken = default);
        Task<Ticket?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default);
        Task<PagedResult<TicketListItem>> GetPagedAsync(TicketQueryOptions options, CancellationToken cancellationToken = default);
        Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default);
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<bool> ExistsAsync(int ticketId, CancellationToken cancellationToken = default);
    }
}
