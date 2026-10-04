namespace HelpDesk.Application.Exceptions;

public class TicketConcurrencyException : Exception
{
    public TicketConcurrencyException(Exception innerException)
        : base(
              "The ticket has been modified by another user. Please reload the ticket and try again.",
              innerException)
    {
    }
}
