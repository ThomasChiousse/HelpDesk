using HelpDesk.Domain;

namespace HelpDesk.Application.Tickets.Queries;

public record TicketListItem(
    int Id,
    string Title,
    TicketPriority Priority,
    TicketStatus Status,
    DateTime CreationDate);