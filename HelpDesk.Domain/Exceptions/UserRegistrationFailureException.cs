namespace HelpDesk.Domain.Exceptions;

public class UserRegistrationFailureException : Exception
{
    public UserRegistrationFailureException() : base("A user with this email already exists")
    {

    }
}
