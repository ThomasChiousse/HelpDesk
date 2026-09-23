using HelpDesk.Application.Authentication;
using HelpDesk.Domain;
using HelpDesk.Tests.Helpers.Authentication;
using HelpDesk.Tests.TestRepositories;

namespace HelpDesk.Tests.ServicesTests;

public class AuthenticationServiceTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeJwtTokenGenerator _jwtTokenGenerator = new();
    private readonly FakePasswordHasher _passwordHasher = new();

    [Fact]
    public async Task Authenticate_WhenUserDoesntExist_ShouldReturnIsSuccessFalse()
    {
        var authService = new AuthenticationService(_userRepository, _passwordHasher, _jwtTokenGenerator);
        var result = await authService.AuthenticateAsync("non_existant@email.com", "fake_pass");

        _passwordHasher.VerificationResult = false;

        Assert.False(result.IsSuccess);
        Assert.Null(result.Token);
    }

    [Fact]
    public async Task Authenticate_UserWithoutPasswordHash_ShouldFail()
    {
        var user = new User(
            42,
            "John",
            "Doe",
            "john@example.com",
            UserRole.Technician);

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        _passwordHasher.VerificationResult = false;

        var authService = new AuthenticationService(_userRepository, _passwordHasher, _jwtTokenGenerator);
        var result = await authService.AuthenticateAsync("non_existant@email.com", "fake_hash");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Token);
    }

    [Fact]
    public async Task Authenticate_WithWrongPassword_ShouldReturnIsSuccessFalse()
    {
        var user = new User(
            42,
            "John",
            "Doe",
            "john@example.com",
            UserRole.Technician);

        user.SetPasswordHash("fake-hash");
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        _passwordHasher.VerificationResult = false;

        var authService = new AuthenticationService(_userRepository, _passwordHasher, _jwtTokenGenerator);
        var result = await authService.AuthenticateAsync("john@example.com", "fake_pass");

        Assert.False(result.IsSuccess);
        Assert.Null(result.Token);
    }

    [Fact]
    public async Task Authenticate_WithCorrectInfos_ShouldReturnToken()
    {
        var user = new User(
            42,
            "John",
            "Doe",
            "john@example.com",
            UserRole.Technician);

        user.SetPasswordHash("fake-hash");
        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        _passwordHasher.VerificationResult = true;

        var authService = new AuthenticationService(_userRepository, _passwordHasher, _jwtTokenGenerator);
        var result = await authService.AuthenticateAsync("john@example.com", "fake-hash");

        Assert.True(result.IsSuccess);
        Assert.Equal(_jwtTokenGenerator.TokenToReturn, result.Token);
        Assert.Equal(user, _jwtTokenGenerator.ReceivedUser);
    }
}
