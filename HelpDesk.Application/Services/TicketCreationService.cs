using HelpDesk.Application.Authentication;
using HelpDesk.Application.Repositories;
using HelpDesk.Domain;

namespace HelpDesk.Application.Services
{
    public class TicketCreationService
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly IUserRepository _userRepository;
        private readonly ICurrentUser _currentUser;

        public TicketCreationService(ITicketRepository ticketRepository, IUserRepository userRepository, ICurrentUser currentUser)
        {
            _ticketRepository = ticketRepository;
            _userRepository = userRepository;
            _currentUser = currentUser;
        }

        public async Task<Ticket> CreateAsync(string title, string description, TicketPriority priority, CancellationToken cancellationToken = default)
        {
            var requester = await _userRepository.GetByIdAsync(_currentUser.Id, cancellationToken) ?? throw new KeyNotFoundException($"User {_currentUser.Id} was not found");
            var ticket = new Ticket(title, description, priority, requester: requester);

            await _ticketRepository.AddAsync(ticket, cancellationToken);
            await _ticketRepository.SaveChangesAsync(cancellationToken);
            return ticket;
        }

    }
}
