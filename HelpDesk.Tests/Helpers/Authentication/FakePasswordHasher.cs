using HelpDesk.Application.Authentication;
using HelpDesk.Domain;

namespace HelpDesk.Tests.Helpers.Authentication;

public class FakePasswordHasher : IPasswordHasher
{
    public bool VerificationResult { get; set; }

    public string Hash(User user, string password)
    {
        throw new NotImplementedException();
    }

    public bool Verify(
    User user,
    string passwordHash,
    string providedPassword)
    {
        return VerificationResult;
    }
}
