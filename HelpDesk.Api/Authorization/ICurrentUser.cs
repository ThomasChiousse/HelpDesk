using HelpDesk.Domain;

namespace HelpDesk.Api.Authorization;

public interface ICurrentUser
{
    int Id { get; }
    UserRole Role { get; }
}
