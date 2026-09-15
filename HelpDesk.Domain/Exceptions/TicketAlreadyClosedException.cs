namespace HelpDesk.Domain.Exceptions;

public class TicketAlreadyClosedException : Exception
{
    public TicketAlreadyClosedException(int ticketId) : base($"Ticket {ticketId} is already closed")
    {
    }
}
