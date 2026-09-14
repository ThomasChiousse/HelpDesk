namespace HelpDesk.Domain.Exceptions
{
    public class TicketHasNoAssigneeException : Exception
    {
        public TicketHasNoAssigneeException(int ticketId) : base($"Ticket with ID {ticketId} has no assigned user.")
        {

        }
    }
}
