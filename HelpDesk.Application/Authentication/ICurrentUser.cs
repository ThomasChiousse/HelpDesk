using HelpDesk.Domain;

namespace HelpDesk.Application.Authentication;

public interface ICurrentUser
{
    int Id { get; }
    UserRole Role { get; }
}
