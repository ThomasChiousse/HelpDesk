using HelpDesk.Application.Repositories;
using HelpDesk.Domain;

namespace HelpDesk.Application.Services;


public class TicketStatusService
{
    private readonly ITicketRepository _ticketRepository;
    public TicketStatusService(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task<TicketStatus> AdvanceStatusAsync(int ticketId, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken) ?? throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");
        ticket.AdvanceStatus();
        await _ticketRepository.SaveChangesAsync(cancellationToken);
        return ticket.Status;
    }
}
