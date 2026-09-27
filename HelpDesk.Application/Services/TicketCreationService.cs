using HelpDesk.Application.Repositories;
using HelpDesk.Domain;

namespace HelpDesk.Application.Services
{
    public class TicketCreationService
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUserRepository _userRepository;

        public TicketCreationService(ITicketRepository ticketRepository, IUserRepository userRepository)
        {
            _ticketRepository = ticketRepository;
            _userRepository = userRepository;
        }

        public async Task<Ticket> CreateAsync(string title, string description, TicketPriority priority, int requesterId, CancellationToken cancellationToken = default)
        {
            var requester = await _userRepository.GetByIdAsync(requesterId, cancellationToken) ?? throw new KeyNotFoundException($"User {requesterId} was not found");
            var ticket = new Ticket(title, description, priority, requester: requester);

            await _ticketRepository.AddAsync(ticket, cancellationToken);
            await _ticketRepository.SaveChangesAsync(cancellationToken);
            return ticket;
        }

    }
}
