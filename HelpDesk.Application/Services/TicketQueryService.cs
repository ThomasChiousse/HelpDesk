using HelpDesk.Application.Repositories;
using HelpDesk.Application.Sorting;
using HelpDesk.Application.Tickets.Queries;
using HelpDesk.Domain;

namespace HelpDesk.Application.Services
{

    public class TicketQueryService
    {
        private readonly ITicketRepository _ticketRepository;
        public TicketQueryService(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Ticket> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var ticket = await _ticketRepository.GetByIdWithDetailsAsync(
                id,
                cancellationToken);

            return ticket is null
                ? throw new KeyNotFoundException(
                    $"Cannot find the ticket with id: {id}")
                : ticket;
        }

        public async Task<(IReadOnlyCollection<TicketListItem> Items, int TotalCount)> GetPagedAsync(TicketStatus? status, TicketPriority? priority, int? assignedUserId, bool? hasAssignee, string? search,
                                                                                            int page, int pageSize, TicketSortField sortField, SortDirection sortDirection, CancellationToken cancellationToken = default)
        {
            return await _ticketRepository.GetPagedAsync(status, priority, assignedUserId, hasAssignee, search, page, pageSize, sortField, sortDirection, cancellationToken);
        }

    }
}
