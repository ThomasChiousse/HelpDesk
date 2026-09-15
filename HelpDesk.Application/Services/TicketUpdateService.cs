using HelpDesk.Application.Repositories;
using HelpDesk.Domain;

namespace HelpDesk.Application.Services;

public class TicketUpdateService
{
    private readonly ITicketRepository _ticketRepository;

    public TicketUpdateService(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<Ticket> UpdateAsync(int ticketId, string title, string description, TicketPriority priority, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken) ?? throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");
        ticket.SetTitle(title);
        ticket.SetDescription(description);
        ticket.SetPriority(priority);
        await _ticketRepository.SaveChangesAsync(cancellationToken);
        return ticket;
    }
}
