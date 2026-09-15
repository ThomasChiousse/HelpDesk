namespace HelpDesk.Domain.Exceptions;

public class TicketClosedException : Exception
{
    public TicketClosedException(int ticketId)
        : base($"Ticket with ID {ticketId} is closed and cannot receive comments.")
    {
    }
}
