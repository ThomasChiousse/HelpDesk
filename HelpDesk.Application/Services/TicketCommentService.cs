using HelpDesk.Application.Repositories;
using HelpDesk.Domain;

namespace HelpDesk.Application.Services;


public class TicketCommentService
{
    private readonly ITicketRepository _ticketRepository;
    private readonly IUserRepository _userRepository;

    public TicketCommentService(ITicketRepository ticketRepository, IUserRepository userRepository)
    {
        _ticketRepository = ticketRepository;
        _userRepository = userRepository;
    }

    public async Task<Comment> AddCommentAsync(int ticketId, int userId, string content, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken) ?? throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken) ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

        var comment = new Comment(user, content);

        ticket.AddComment(comment);
        await _ticketRepository.SaveChangesAsync(cancellationToken);
        return comment;
    }

}
