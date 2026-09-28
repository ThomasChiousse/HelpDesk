using HelpDesk.Application.Authentication;
using HelpDesk.Domain;

public sealed class FakeCurrentUser : ICurrentUser
{
    public int Id { get; set; }
    public UserRole Role { get; set; }
}