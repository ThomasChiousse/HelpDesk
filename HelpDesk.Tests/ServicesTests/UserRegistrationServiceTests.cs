using HelpDesk.Application.Exceptions;
using HelpDesk.Application.Services.Users;
using HelpDesk.Domain;
using HelpDesk.Tests.Helpers.Authentication;
using HelpDesk.Tests.TestRepositories;

namespace HelpDesk.Tests.ServicesTests;

public class UserRegistrationServiceTests
{
    [Fact]
    public async Task RegisterUser_WithValidData_ShouldCreateUser()
    {
        string firstname = "John",
            lastname = "Doe",
            email = "email@example.com";

        FakePasswordHasher fakePasswordHasher = new();
        FakeUserRepository fakeUserRepository = new();
        UserRegistrationService userRegistrationService = new(fakeUserRepository, fakePasswordHasher);
        fakePasswordHasher.HashResult = "#HASH_RESULT#";
        const string password = "fake-password";

        var createdUser = await userRegistrationService.RegisterAsync(firstname, lastname, email, password);

        Assert.Equal(firstname, createdUser.Firstname);
        Assert.Equal(lastname, createdUser.Lastname);
        Assert.Equal(email, createdUser.Email);
        Assert.Equal(UserRole.User, createdUser.Role);
        Assert.Equal("#HASH_RESULT#", createdUser.PasswordHash);
        Assert.NotEqual(password, createdUser.PasswordHash);
        Assert.Equal(password, fakePasswordHasher.LastProvidedPassword);
        Assert.Same(createdUser, fakePasswordHasher.LastHashedUser);
        Assert.True(fakeUserRepository.SaveChangesCalled);
        Assert.Same(createdUser, await fakeUserRepository.GetByEmailAsync(email));
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowUserEmailAlreadyInUseException()
    {
        FakePasswordHasher fakePasswordHasher = new();
        FakeUserRepository fakeUserRepository = new();
        UserRegistrationService userRegistrationService = new(fakeUserRepository, fakePasswordHasher);
        string firstname = "John", lastname = "Doe";
        string sameEmail = "john@email.com";

        await fakeUserRepository.AddAsync(new User(firstname, lastname, sameEmail, UserRole.User));

        await Assert.ThrowsAsync<UserEmailAlreadyInUseException>(async () => await userRegistrationService.RegisterAsync("John2", "SomeLastname", sameEmail, "some-password"));
        Assert.False(fakeUserRepository.SaveChangesCalled);
        Assert.Null(fakePasswordHasher.LastHashedUser);
        Assert.Null(fakePasswordHasher.LastProvidedPassword);
    }
}
