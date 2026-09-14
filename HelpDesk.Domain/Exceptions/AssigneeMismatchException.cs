namespace HelpDesk.Domain.Exceptions
{
    public class AssigneeMismatchException : Exception
    {
        public AssigneeMismatchException(
        int requestedUserId,
        int assignedUserId)
        : base(
            $"User {requestedUserId} is not assigned to this ticket. " +
            $"The assigned user is {assignedUserId}.")
        {

        }
    }
}
