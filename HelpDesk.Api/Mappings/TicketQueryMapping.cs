using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Application.Sorting;
using HelpDesk.Application.Tickets.Queries;
using HelpDesk.Domain;

namespace HelpDesk.Api.Mappings;

public static class TicketQueryMapping
{
    public static TicketQueryMappingResult ToQueryOptions(this GetTicketsRequest request)
    {
        List<ValidationError> validationErrors = [];

        if (request.HasAssignee == false
            && request.AssignedUserId is not null)
        {
            validationErrors.Add(new ValidationError(
                nameof(request.AssignedUserId),
                "AssignedUserId cannot be used when HasAssignee is false."));
        }

        TicketStatus? status = null;

        if (request.Status is not null)
        {
            if (!Enum.TryParse<TicketStatus>(
                    request.Status,
                    ignoreCase: true,
                    out var parsedStatus)
                || !Enum.IsDefined(parsedStatus))
            {
                validationErrors.Add(new ValidationError(
                    nameof(request.Status),
                    "Unknown ticket status."));
            }
            else
            {
                status = parsedStatus;
            }
        }

        TicketPriority? priority = null;

        if (request.Priority is not null)
        {
            if (!Enum.TryParse<TicketPriority>(
                    request.Priority,
                    ignoreCase: true,
                    out var parsedPriority)
                || !Enum.IsDefined(parsedPriority))
            {
                validationErrors.Add(new ValidationError(
                    nameof(request.Priority),
                    "Unknown ticket priority."));
            }
            else
            {
                priority = parsedPriority;
            }
        }

        SortDirection sortDirection = SortDirection.Descending;

        switch (request.SortDirection?.ToLowerInvariant())
        {
            case null:
            case "desc":
                sortDirection = SortDirection.Descending;
                break;

            case "asc":
                sortDirection = SortDirection.Ascending;
                break;

            default:
                validationErrors.Add(new ValidationError(
                     nameof(request.SortDirection),
                     "SortDirection must be 'asc' or 'desc'."));
                break;
        }

        TicketSortField sortField = TicketSortField.CreationDate;

        switch (request.SortBy?.ToLowerInvariant())
        {
            case null:
            case "creationdate":
                sortField = TicketSortField.CreationDate;
                break;

            case "title":
                sortField = TicketSortField.Title;
                break;

            case "priority":
                sortField = TicketSortField.Priority;
                break;

            case "status":
                sortField = TicketSortField.Status;
                break;

            default:
                validationErrors.Add(new ValidationError(
                    nameof(request.SortBy),
                    "SortBy must be 'creationDate', 'title', 'priority' or 'status'."));
                break;
        }


        if (validationErrors.Count > 0)
        {
            return new TicketQueryMappingResult(
                null,
                validationErrors);
        }

        var options = new TicketQueryOptions
        {
            Status = status,
            Priority = priority,
            AssignedUserId = request.AssignedUserId,
            HasAssignee = request.HasAssignee,
            Search = request.Search,
            Page = request.Page,
            PageSize = request.PageSize,
            SortField = sortField,
            SortDirection = sortDirection
        };

        return new TicketQueryMappingResult(options, validationErrors);
    }
}
