using HelpDesk.Application.Repositories;
using HelpDesk.Domain;

namespace HelpDesk.Application.Services
{
    public class TicketCreationService
    {
        private readonly ITicketRepository _ticketRepository;

        public TicketCreationService(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public async Task<Ticket> CreateAsync(string title, string description, TicketPriority priority, CancellationToken cancellationToken = default)
        {
            var ticket = new Ticket(title, description, priority);

            await _ticketRepository.AddAsync(ticket, cancellationToken);
            await _ticketRepository.SaveChangesAsync(cancellationToken);
            return ticket;
        }

    }
}
