using HelpDesk.Application.Authentication;
using HelpDesk.Domain;

namespace HelpDesk.Tests.Helpers.Authentication;

public sealed class FakeCurrentUser : ICurrentUser
{
    public int Id { get; set; }
    public UserRole Role { get; set; }
}