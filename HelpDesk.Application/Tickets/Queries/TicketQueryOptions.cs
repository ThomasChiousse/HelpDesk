using HelpDesk.Application.Sorting;
using HelpDesk.Domain;

namespace HelpDesk.Application.Tickets.Queries;

public sealed record TicketQueryOptions
{
    public TicketStatus? Status { get; init; }
    public TicketPriority? Priority { get; init; }

    public int? AssignedUserId { get; init; }
    public bool? HasAssignee { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;

    public TicketSortField SortField { get; init; }
        = TicketSortField.CreationDate;

    public SortDirection SortDirection { get; init; }
        = SortDirection.Descending;
}