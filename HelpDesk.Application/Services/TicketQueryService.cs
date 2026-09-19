using HelpDesk.Application.Common.Pagination;
using HelpDesk.Application.Repositories;
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

        public async Task<PagedResult<TicketListItem>> GetPagedAsync(TicketQueryOptions options, CancellationToken cancellationToken = default)
        {
            return await _ticketRepository.GetPagedAsync(options, cancellationToken);
        }

    }
}
