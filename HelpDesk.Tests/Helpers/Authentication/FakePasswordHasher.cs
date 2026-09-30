using HelpDesk.Application.Authentication;
using HelpDesk.Domain;

namespace HelpDesk.Tests.Helpers.Authentication;

public class FakePasswordHasher : IPasswordHasher
{
    public string HashResult { get; set; } = "fake-hashed-password";

    public User? LastHashedUser { get; private set; }
    public string? LastProvidedPassword { get; private set; }

    public bool VerificationResult { get; set; }

    public string Hash(User user, string password)
    {
        LastHashedUser = user;
        LastProvidedPassword = password;

        return HashResult;
    }

    public bool Verify(
        User user,
        string passwordHash,
        string providedPassword)
    {
        return VerificationResult;
    }
}
