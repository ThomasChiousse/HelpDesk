using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Domain;

namespace HelpDesk.Api.Mappings;

public static class TicketMappings
{
    public static TicketDetailsResponse ToDetailsResponse(this Ticket ticket)
    {
        return new TicketDetailsResponse(
            ticket.Id,
            ticket.Title,
            ticket.Description,
            ticket.Priority.ToString(),
            ticket.Status.ToString(),
            ticket.CreationDate,
            ticket.AssignedUser is null
                ? null
                : new UserResponse(
                    ticket.AssignedUser.Id,
                    ticket.AssignedUser.Firstname,
                    ticket.AssignedUser.Lastname),
            ticket.Comments
                .Select(c => new CommentResponse(
                    c.Id,
                    c.Content,
                    c.CreationDate,
                    new UserResponse(
                        c.Author.Id,
                        c.Author.Firstname,
                        c.Author.Lastname)))
                .ToList());
    }
}