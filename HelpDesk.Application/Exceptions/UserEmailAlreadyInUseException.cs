namespace HelpDesk.Application.Exceptions;

public class UserEmailAlreadyInUseException : Exception
{
    public UserEmailAlreadyInUseException() : base("A user with this email already exists")
    {

    }
}