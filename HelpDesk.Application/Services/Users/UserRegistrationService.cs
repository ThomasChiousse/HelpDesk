using HelpDesk.Application.Authentication;
using HelpDesk.Application.Repositories;
using HelpDesk.Domain;
using HelpDesk.Domain.Exceptions;

namespace HelpDesk.Application.Services.Users;

public class UserRegistrationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    public UserRegistrationService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task RegisterAsync(string firstname, string lastname, string email, string password, CancellationToken cancellationToken = default)
    {

        if (await _userRepository.GetByEmailAsync(email, cancellationToken) != null)
        {
            throw new UserRegistrationFailureException();
        }

        var user = new User(firstname, lastname, email, UserRole.User);
        var passwordHash = _passwordHasher.Hash(user, password);
        user.SetPasswordHash(passwordHash);

        await _userRepository.AddAsync(user, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);
    }
}
