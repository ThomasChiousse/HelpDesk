using HelpDesk.Domain;

namespace HelpDesk.Application.Authentication;

public interface IPasswordHasher
{
    string Hash(User user, string password);

    bool Verify(
        User user,
        string passwordHash,
        string providedPassword);
}