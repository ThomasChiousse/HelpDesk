using HelpDesk.Application.Authentication;
using HelpDesk.Domain;

namespace HelpDesk.Tests.Helpers.Authentication;

public class FakeJwtTokenGenerator : IJwtTokenGenerator
{
    public string TokenToReturn { get; set; } = "fake-token";

    public User? ReceivedUser { get; private set; }

    public string Generate(User user)
    {
        ReceivedUser = user;
        return TokenToReturn;
    }
}
