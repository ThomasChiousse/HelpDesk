using HelpDesk.Application.Repositories;

namespace HelpDesk.Application.Authentication;

public sealed class AuthenticationService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public AuthenticationService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user is null) return new AuthenticationResult(false, null);

        var passwordHash = user.PasswordHash;
        if (passwordHash is null) return new AuthenticationResult(false, null);

        if (!_passwordHasher.Verify(user, passwordHash, password))
        {
            return new AuthenticationResult(false, null);
        }

        return new AuthenticationResult(true, _jwtTokenGenerator.Generate(user));
    }
}