using HelpDesk.Application.Repositories;
using HelpDesk.Domain;

namespace HelpDesk.Application.Services;

public class TicketPatchService
{
    private readonly ITicketRepository _ticketRepository;
    public TicketPatchService(ITicketRepository ticketRepository)
    {
        _ticketRepository = ticketRepository;
    }

    public async Task PatchAsync(int ticketId, string? title = null, string? description = null, TicketPriority? priority = null, CancellationToken cancellationToken = default)
    {
        var ticket = await _ticketRepository.GetByIdAsync(ticketId, cancellationToken) ?? throw new KeyNotFoundException($"Ticket with ID {ticketId} not found.");
        if (title is not null)
            ticket.SetTitle(title);
        if (description is not null)
            ticket.SetDescription(description);
        if (priority is not null)
            ticket.SetPriority(priority.Value);
        await _ticketRepository.SaveChangesAsync(cancellationToken);
    }
}
