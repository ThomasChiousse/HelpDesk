using HelpDesk.Api.Tests.Helpers.Authentication;
using HelpDesk.Application.Authentication;
using HelpDesk.Domain;
using HelpDesk.Tests.TestRepositories;

namespace HelpDesk.Tests.ServicesTests;

public class AuthenticationServiceTests
{
    private readonly FakeUserRepository _userRepository = new();
    private readonly FakeJwtTokenGenerator _jwtTokenGenerator = new();
    private readonly FakePasswordHasher _passwordHasher = new();

    [Fact]
    public async Task Login_WhenUserDoesntExist_ShouldReturnIsSuccessFalse()
    {
        var authService = new AuthenticationService(_userRepository, _passwordHasher, _jwtTokenGenerator);
        var result = await authService.AuthenticateAsync("non_existant@email.com", "fake_pass");
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Login_WithWrongEmail_ShouldReturnIsSuccessFalse()
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

        var authService = new AuthenticationService(_userRepository, _passwordHasher, _jwtTokenGenerator);
        var result = await authService.AuthenticateAsync("non_existant@email.com", "fake_hash");
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShouldReturnIsSuccessFalse()
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

        var authService = new AuthenticationService(_userRepository, _passwordHasher, _jwtTokenGenerator);
        var result = await authService.AuthenticateAsync("john@example.com", "fake_pass");
        Assert.False(result.IsSuccess);
    }

    [Fact]
    public async Task Login_WithCorrectInfos_ShouldReturnToken()
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

        var authService = new AuthenticationService(_userRepository, _passwordHasher, _jwtTokenGenerator);
        var result = await authService.AuthenticateAsync("john@example.com", "fake-hash");
        Assert.True(result.IsSuccess);
        var fake_token = _jwtTokenGenerator.Generate(user);
        Assert.Equal(fake_token, result.Token);
        Assert.Equal(user, _jwtTokenGenerator.ReceivedUser);
    }
}
