using HelpDesk.Application.Authentication;
using HelpDesk.Application.Repositories;
using HelpDesk.Domain;

namespace HelpDesk.Application.Services;


public class TicketCommentService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUser _currentUser;

    public TicketCommentService(ITicketRepository ticketRepository, IUserRepository userRepository, ICurrentUser currentUser)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
        _currentUser = currentUser;
    }

    public async Task<Comment> AddCommentAsync(int ticketId, string content, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken) ?? throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");
        var user = await _userRepository.GetByIdAsync(_currentUser.Id, cancellationToken) ?? throw new KeyNotFoundException($"User with ID {_currentUser.Id} not found.");

        var comment = new Comment(user, content);

        ticket.AddComment(comment);
        await _ticketRepository.SaveChangesAsync(cancellationToken);
        return comment;
    }

}
