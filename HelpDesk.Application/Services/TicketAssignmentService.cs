using HelpDesk.Application.Repositories;

namespace HelpDesk.Application.Services
{
    public class TicketAssignmentService
    {
        private readonly IUserRepository _userRepository;
        private readonly ITicketRepository _ticketRepository;

        public TicketAssignmentService(
        ITicketRepository ticketRepository,
        IUserRepository userRepository)
        {
            _ticketRepository = ticketRepository;
            _userRepository = userRepository;
        }

        public async Task AssignUserAsync(
        int ticketId,
        int userId,
        CancellationToken cancellationToken = default)
        {
            // 1. récupérer le ticket
            // 2. si inexistant → exception
            var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken) ?? throw new KeyNotFoundException($"Ticket with id {ticketId} was not found.");
            // 3. récupérer l'utilisateur
            // 4. si inexistant → exception
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken) ?? throw new KeyNotFoundException($"User with id {userId} was not found");
            // 5. utiliser ticket.AssignUser(...)
            ticket.AssignUser(user);
            // 6. sauvegarder
            await _ticketRepository.SaveChangesAsync(cancellationToken);
        }

        public async Task UnassignUserAsync(int ticketId, int userId, CancellationToken cancellationToken = default)
        {
            var ticket = await _ticketRepository.GetByIdWithAssigneeAsync(ticketId, cancellationToken) ?? throw new KeyNotFoundException($"Ticket with id {ticketId} was not found");

            ticket.UnassignUser(userId);
            await _ticketRepository.SaveChangesAsync(cancellationToken);
        }
    }
}
