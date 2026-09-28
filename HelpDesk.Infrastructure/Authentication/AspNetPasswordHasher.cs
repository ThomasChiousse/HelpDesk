using HelpDesk.Application.Authentication;
using HelpDesk.Domain;
using Microsoft.AspNetCore.Identity;

namespace HelpDesk.Infrastructure.Authentication;

public sealed class AspNetPasswordHasher : IPasswordHasher
{
    private readonly PasswordHasher<User> _hasher = new();

    public string Hash(User user, string password)
    {
        return _hasher.HashPassword(user, password);
    }

    public bool Verify(User user, string passwordHash, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(
            user,
            passwordHash,
            providedPassword);

        return result != PasswordVerificationResult.Failed;
    }
}