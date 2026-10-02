using HelpDesk.Api.Contracts.Users;
using HelpDesk.Domain;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace HelpDesk.Api.Tests;

public class UsersEndpointsTests
{
    [Fact]
    public async Task RegisterUser_WithValidRequest_ShouldReturn201Created()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        var request = new RegisterUserRequest
        {
            Firstname = "John",
            Lastname = "Doe",
            Email = "john.doe@example.com",
            Password = "Ultr@SecureP@ssw0rd"
        };

        var response = await client.PostAsJsonAsync("/api/users", request);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var userResponse = await response.Content.ReadFromJsonAsync<UserResponse>();
        Assert.NotNull(userResponse);
        Assert.Equal(request.Firstname, userResponse.Firstname);
        Assert.Equal(request.Lastname, userResponse.Lastname);
        Assert.Equal(request.Email, userResponse.Email);
        Assert.Equal(UserRole.User.ToString(), userResponse.Role);
        Assert.True(userResponse.Id > 0);

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            var userInDb = await context.Users.FindAsync(userResponse.Id);
            Assert.NotNull(userInDb);
            Assert.Equal(request.Firstname, userInDb.Firstname);
            Assert.Equal(request.Lastname, userInDb.Lastname);
            Assert.Equal(request.Email, userInDb.Email);
            Assert.Equal(UserRole.User, userInDb.Role);
            Assert.NotNull(userInDb.PasswordHash);
            Assert.NotEqual(request.Password, userInDb.PasswordHash);
        }
    }

    [Fact]
    public async Task RegisterUser_WithDuplicateEmail_ShouldReturn409BadConflict()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        var request = new RegisterUserRequest
        {
            Firstname = "Jane",
            Lastname = "Doe",
            Email = "jane.doe@example.com",
            Password = "Ultr@SecureP@ssw0rd"
        };


        await client.PostAsJsonAsync("/api/users", request);

        var response = await client.PostAsJsonAsync("/api/users", request);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(409, problem.Status);
        Assert.Equal("Conflict", problem.Title);
        Assert.Contains("this email already exists", problem.Detail, StringComparison.OrdinalIgnoreCase);

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            Assert.Single(context.Users.Where(u => u.Email == request.Email));
            Assert.Equal(1, context.Users.Count());
        }
    }

    [Theory]
    [InlineData("", "Doe", "john.doe@example.com", "Ultr@SecureP@ssw0rd")]
    [InlineData("John", "", "john.doe@example.com", "Ultr@SecureP@ssw0rd")]
    [InlineData("John", "Doe", "notanemail", "Ultr@SecureP@ssw0rd")]
    [InlineData("John", "Doe", "john.doe@example.com", "")]
    [InlineData("John", "Doe", "john.doe@example.com", "2Short")]
    public async Task RegisterUser_WithInvalidRequest_ShouldReturn400BadRequest(string firstname, string lastname, string email, string password)
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        var request = new RegisterUserRequest
        {
            Firstname = firstname,
            Lastname = lastname,
            Email = email,
            Password = password
        };

        var response = await client.PostAsJsonAsync("/api/users", request);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
