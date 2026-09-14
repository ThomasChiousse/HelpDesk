using HelpDesk.Application.Repositories;
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

    }
}
