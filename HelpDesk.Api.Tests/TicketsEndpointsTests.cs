using HelpDesk.Api.Contracts.Auth;
using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Application.Authentication;
using HelpDesk.Domain;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace HelpDesk.Api.Tests;

public class TicketsEndpointsTests
{
    #region Helpers
    private static async Task<int> SeedUserWithPasswordAsync(
    HelpDeskApiFactory factory,
    string email = "john@example.com",
    string password = "correct-password")
    {
        using var scope = factory.Services.CreateScope();

        var context = scope.ServiceProvider
            .GetRequiredService<HelpDeskDbContext>();

        var passwordHasher = scope.ServiceProvider
            .GetRequiredService<IPasswordHasher>();

        var user = new User(
            "John",
            "Doe",
            email,
            UserRole.Technician);

        var hash = passwordHasher.Hash(
            user,
            password);

        user.SetPasswordHash(hash);

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return user.Id;
    }
    #endregion

    #region PostTicket
    [Fact]
    public async Task PostTicket_WithValidRequest_ShouldReturnCreated()
    {
        // arrange
        using var factory = new HelpDeskApiFactory();

        var client = factory.CreateClient();
        var request = new
        {
            title = "Keyboard broken",
            description = "Several keys do not work",
            priority = "High"
        };

        // act
        var response = await client.PostAsJsonAsync("/api/tickets", request);

        var ticket = await response.Content.ReadFromJsonAsync<TicketDetailsResponse>();

        // assert
        Assert.NotNull(ticket);
        Assert.True(ticket.Id > 0);
        Assert.Equal("Keyboard broken", ticket.Title);
        Assert.Equal("Several keys do not work", ticket.Description);
        Assert.Equal("High", ticket.Priority);
        Assert.Equal("Open", ticket.Status);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/api/tickets/{ticket.Id}", response.Headers.Location.ToString());

        var getResponse = await client.GetAsync($"/api/tickets/{ticket?.Id}");
        var retrievedTicket = await getResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.NotNull(retrievedTicket);

        Assert.Equal(ticket.Id, retrievedTicket.Id);
        Assert.Equal(ticket.Title, retrievedTicket.Title);

    }

    [Theory]
    [InlineData("4")]
    [InlineData("-45")]
    [InlineData("Banana")]
    public async Task PostTicket_WithInvalidPriority_ShouldReturnBadRequest(string priority)
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        var request = new
        {
            title = "Keyboard",
            description = "Broken",
            priority
        };

        var response = await client.PostAsJsonAsync("/api/tickets", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    #endregion

    #region GetTicket
    [Theory]
    [InlineData(0)]
    [InlineData(-14)]
    [InlineData(124)]
    public async Task GetTicket_WithUnknownId_ShouldReturnNotFound(int id)
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        var getResponse = await client.GetAsync($"/api/tickets/{id}");

        var problem = await getResponse.Content
     .ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
        Assert.Equal("Resource not found", problem.Title);
        Assert.Contains(id.ToString(), problem.Detail);
    }
    #endregion

    #region AssignUser
    [Fact]
    public async Task AssignUser_WithValidRequest_ShouldAssignUser()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        int userId;
        int ticketId;

        using (var scope = factory.Services.CreateScope())
        {

            var context = scope.ServiceProvider
                .GetRequiredService<HelpDeskDbContext>();

            var user = new User(
                "Thomas",
                "Banana",
                "email@domain.com",
                UserRole.Technician);

            var ticket = new Ticket(
                "Printer broken",
                "The printer doesn't work",
                TicketPriority.Normal);

            context.Users.Add(user);
            context.Tickets.Add(ticket);

            await context.SaveChangesAsync();

            userId = user.Id;
            ticketId = ticket.Id;

            Assert.Null(ticket.AssignedUser);
        }

        var putResponse = await client.PutAsync($"/api/tickets/{ticketId}/assignee/{userId}", content: null);
        Assert.Equal(HttpStatusCode.NoContent, putResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/tickets/{ticketId}");
        var ticket2 = await getResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();

        Assert.NotNull(ticket2);
        Assert.NotNull(ticket2.AssignedUser);
        Assert.Equal(userId, ticket2.AssignedUser.Id);
    }

    [Fact]
    public async Task AssignUser_WithUnknownTicket_ShouldReturnNotFound()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        int userId;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();

            var user = new User(
                "Thomas",
                "Banana",
                "email@domain.com",
                UserRole.Technician);
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            userId = user.Id;
        }

        var putResponse = await client.PutAsync($"/api/tickets/999/assignee/{userId}", null);
        Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);
    }

    [Fact]
    public async Task AssignUser_WithUnknownUser_ShouldReturnNotFound()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        int ticketId;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();

            var ticket = new Ticket(
                "Printer not working",
                "Some kind of ink problem",
                TicketPriority.Normal);

            await context.Tickets.AddAsync(ticket);
            await context.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var putResponse = await client.PutAsync($"/api/tickets/{ticketId}/assignee/999", null);
        Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);
    }
    #endregion

    #region UnassignUser
    [Fact]
    public async Task UnassignUser_WithValidData_ShouldUnassignUser()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        int ticketId, userId;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();

            var user = new User(
                "Thomas",
                "Banana",
                "email@domain.com",
                UserRole.Technician);

            var ticket = new Ticket(
                "Printer broken",
                "The printer doesn't work",
                TicketPriority.Normal);

            ticket.AssignUser(user);

            await context.Tickets.AddAsync(ticket);
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            ticketId = ticket.Id;
            userId = user.Id;
        }

        var deleteResponse = await client.DeleteAsync($"/api/tickets/{ticketId}/assignee/{userId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var ticketFromDb = await getResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();


        Assert.NotNull(ticketFromDb);
        Assert.Null(ticketFromDb.AssignedUser);
    }

    [Fact]
    public async Task UnassignUser_WhenUserIsNotAssigned_ShouldReturnConflict()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        int ticketId, user1Id, user2Id;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();

            var user1 = new User(
                "Thomas",
                "Banana",
                "email@domain.com",
                UserRole.Technician);

            var user2 = new User(
                "Tom",
                "Ban",
                "email2@domain.com",
                UserRole.Technician);

            var ticket = new Ticket(
                "Printer broken",
                "The printer doesn't work",
                TicketPriority.Normal);

            ticket.AssignUser(user1);

            await context.Tickets.AddAsync(ticket);
            await context.Users.AddAsync(user1);
            await context.Users.AddAsync(user2);
            await context.SaveChangesAsync();

            ticketId = ticket.Id;
            user1Id = user1.Id;
            user2Id = user2.Id;
        }

        var deleteResponse = await client.DeleteAsync($"/api/tickets/{ticketId}/assignee/{user2Id}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var ticketFromDb = await getResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();

        Assert.NotNull(ticketFromDb);
        Assert.NotNull(ticketFromDb.AssignedUser);
        Assert.Equal(user1Id, ticketFromDb.AssignedUser.Id);
    }

    [Fact]
    public async Task UnassignUser_WhenTicketHasNoAssignedUser_ShouldReturnConflict()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        int ticketId;
        Ticket ticket;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();

            ticket = new Ticket(
                "Printer broken",
                "The printer doesn't work",
                TicketPriority.Normal);

            await context.Tickets.AddAsync(ticket);
            await context.SaveChangesAsync();

            ticketId = ticket.Id;
        }

        var deleteResponse = await client.DeleteAsync($"/api/tickets/{ticketId}/assignee/999");
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
    }


    [Fact]
    public async Task UnassignUser_WithUnknownTicket_ShouldReturnNotFound()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        var deleteResponse = await client.DeleteAsync($"/api/tickets/999/assignee/999");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }
    #endregion

    #region AddComment
    [Fact]
    public async Task AddComment_WithAuthenticatedUser_ShouldUseAuthenticatedUserAsAuthor()
    {
        using var factory = new HelpDeskApiFactory();
        var userId = await SeedUserWithPasswordAsync(factory);
        var client = factory.CreateClient();

        var loginRequest = new LoginRequest("john@example.com", "correct-password");
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);

        var token = loginResponse.Token;

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        int ticketId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            var ticket = new Ticket("Broken mouse", "No more left clicking", TicketPriority.Normal);

            await context.Tickets.AddAsync(ticket);
            await context.SaveChangesAsync();

            ticketId = ticket.Id;
        }

        var postRequest = new CreateCommentRequest
        {
            Content = "This seems like a very important ticket"
        };

        var postResponse = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/comments", postRequest);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var createdComment = await postResponse.Content.ReadFromJsonAsync<CommentResponse>();
        Assert.NotNull(createdComment);
        Assert.True(createdComment.Id > 0);
        Assert.Equal(userId, createdComment.Author.Id);
        Assert.Equal("This seems like a very important ticket", createdComment.Content);

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<HelpDeskDbContext>();

            var persistedComment = await context.Comments
                .Include(c => c.Author)
                .SingleAsync(c => c.Id == createdComment.Id);

            Assert.Equal(createdComment.Id, persistedComment.Id);
            Assert.Equal(userId, persistedComment.Author.Id);
            Assert.Equal("This seems like a very important ticket", persistedComment.Content);
        }
    }

    [Fact]
    public async Task AddComment_WithUnknownTicket_ShouldReturnNotFound()
    {
        using var factory = new HelpDeskApiFactory();
        var userId = await SeedUserWithPasswordAsync(factory);
        var client = factory.CreateClient();

        var loginRequest = new LoginRequest("john@example.com", "correct-password");
        var loginPostResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.NotNull(loginPostResponse);

        var loginResponse = await loginPostResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);

        var token = loginResponse.Token;

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            Content = "This is a comment"
        };
        var postResponse = await client.PostAsJsonAsync($"/api/tickets/999/comments", request);
        Assert.Equal(HttpStatusCode.NotFound, postResponse.StatusCode);
    }

    [Fact]
    public async Task AddComment_ToClosedTicket_ShouldReturnConflict()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        int ticketId;
        int userId = await SeedUserWithPasswordAsync(factory);
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            var ticket = new Ticket(
                "Printer broken",
                "The printer doesn't work",
                TicketPriority.Normal);

            ticket.AdvanceStatus(); // open -> in progress
            ticket.AdvanceStatus(); // in progress -> resolved
            ticket.AdvanceStatus(); // resolved -> closed

            await context.Tickets.AddAsync(ticket);
            await context.SaveChangesAsync();

            ticketId = ticket.Id;
        }

        var loginRequest = new LoginRequest("john@example.com", "correct-password");
        var loginPostResponse = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.NotNull(loginPostResponse);

        var loginResponse = await loginPostResponse.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);

        var token = loginResponse.Token;

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var request = new
        {
            Content = "This is a comment"
        };
        var postResponse = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/comments", request);
        Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<HelpDeskDbContext>();

            Assert.Empty(await context.Comments.ToListAsync());
        }
    }

    [Fact]
    public async Task AddComment_WithoutToken_ShouldReturnUnauthorized()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        int ticketId;
        int userId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            var ticket = new Ticket(
                "Printer broken",
                "The printer doesn't work",
                TicketPriority.Normal);
            var user = new User("Thomas", "Banana", "thomas.banana@example.com", UserRole.Technician);
            await context.Tickets.AddAsync(ticket);

            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();

            ticketId = ticket.Id;
            userId = user.Id;

        }

        var request = new
        {
            AuthorId = userId,
            Content = "This is a comment"
        };
        var postResponse = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/comments", request);
        Assert.Equal(HttpStatusCode.Unauthorized, postResponse.StatusCode);
    }



    [Fact]
    public async Task AddComment_WithDifferentAuthorIdInBody_ShouldNotAllowImpersonation()
    {
        using var factory = new HelpDeskApiFactory();
        var userId = await SeedUserWithPasswordAsync(factory);

        var client = factory.CreateClient();

        var loginRequest = new LoginRequest("john@example.com", "correct-password");
        var response = await client.PostAsJsonAsync("/api/auth/login", loginRequest);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(loginResponse);

        var token = loginResponse.Token;

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        int ticketId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            var ticket = new Ticket("Broken mouse", "No more left clicking", TicketPriority.Normal);

            await context.Tickets.AddAsync(ticket);
            await context.SaveChangesAsync();

            ticketId = ticket.Id;
        }

        var postRequest = new
        {
            AuthorId = 999,
            Content = "This seems like a very important ticket"
        };
        var postResponse = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/comments", postRequest);
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var commentResponse = await postResponse.Content.ReadFromJsonAsync<CommentResponse>();
        Assert.NotNull(commentResponse);
        Assert.NotEqual(999, commentResponse.Author.Id);
        Assert.Equal(userId, commentResponse.Author.Id);

    }
    #endregion

    #region AdvanceStatus
    [Fact]
    public async Task AdvanceStatus_WithValidRequest_ShouldAdvanceStatus()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        int ticketId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            var ticket = new Ticket(
                "Printer broken",
                "The printer doesn't work",
                TicketPriority.Normal);
            await context.Tickets.AddAsync(ticket);
            await context.SaveChangesAsync();
            ticketId = ticket.Id;
        }
        var patchResponse = await client.PatchAsync($"/api/tickets/{ticketId}/status", null);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var advancedStatus = await patchResponse.Content.ReadFromJsonAsync<TicketStatusResponse>();
        Assert.NotNull(advancedStatus);
        Assert.Equal("InProgress", advancedStatus.Status);
        var getResponse = await client.GetAsync($"/api/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var ticketFromDb = await getResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();
        Assert.NotNull(ticketFromDb);
        Assert.Equal(TicketStatus.InProgress, Enum.Parse<TicketStatus>(ticketFromDb.Status));
    }

    [Fact]
    public async Task AdvanceStatus_WhenTicketIsAlreadyClosed_ShouldReturnConflict()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        int ticketId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            var ticket = new Ticket(
                "Printer broken",
                "The printer doesn't work",
                TicketPriority.Normal);
            ticket.AdvanceStatus(); // open -> in progress
            ticket.AdvanceStatus(); // in progress -> resolved
            ticket.AdvanceStatus(); // resolved -> closed
            await context.Tickets.AddAsync(ticket);
            await context.SaveChangesAsync();
            ticketId = ticket.Id;
        }
        var patchResponse = await client.PatchAsync($"/api/tickets/{ticketId}/status", null);
        Assert.Equal(HttpStatusCode.Conflict, patchResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var ticketFromDb = await getResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();
        Assert.NotNull(ticketFromDb);
        Assert.Equal(TicketStatus.Closed, Enum.Parse<TicketStatus>(ticketFromDb.Status));
    }

    [Fact]
    public async Task AdvanceStatus_WithUnknownTicket_ShouldReturnNotFound()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        var patchResponse = await client.PatchAsync($"/api/tickets/999/status", null);
        Assert.Equal(HttpStatusCode.NotFound, patchResponse.StatusCode);
    }
    #endregion

    #region UpdateTicket
    [Fact]
    public async Task UpdateTicket_WithValidRequest_ShouldUpdateTicket()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        int ticketId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            var ticket = new Ticket(
                "Old Title",
                "Old Description",
                TicketPriority.Normal);
            await context.Tickets.AddAsync(ticket);
            await context.SaveChangesAsync();
            ticketId = ticket.Id;
        }
        var updateRequest = new UpdateTicketRequest
        {
            Title = "New Title",
            Description = "New Description",
            Priority = "High"
        };
        var updateResponse = await client.PutAsJsonAsync($"/api/tickets/{ticketId}", updateRequest);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedTicket = await updateResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();

        Assert.NotNull(updatedTicket);
        Assert.Equal("New Title", updatedTicket.Title);
        Assert.Equal("New Description", updatedTicket.Description);
        Assert.Equal("High", updatedTicket.Priority);

        var getResponse = await client.GetAsync($"/api/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var ticketFromDb = await getResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();
        Assert.NotNull(ticketFromDb);
        Assert.Equal("New Title", ticketFromDb.Title);
        Assert.Equal("New Description", ticketFromDb.Description);
        Assert.Equal("High", ticketFromDb.Priority);
    }

    [Fact]
    public async Task UpdateTicket_WithUnknownTicket_ShouldReturnNotFound()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        var updateRequest = new UpdateTicketRequest
        {
            Title = "New Title",
            Description = "New Description",
            Priority = "High"
        };
        var updateResponse = await client.PutAsJsonAsync($"/api/tickets/999", updateRequest);
        Assert.Equal(HttpStatusCode.NotFound, updateResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateTicket_WithInvalidPriority_ShouldReturnBadRequest()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        var updateRequest = new UpdateTicketRequest
        {
            Title = "New Title",
            Description = "New Description",
            Priority = "Invalid"
        };
        var updateResponse = await client.PutAsJsonAsync($"/api/tickets/1", updateRequest);
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateTicket_WithInvalidTitle_ShouldReturnBadRequest()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        var updateRequest = new UpdateTicketRequest
        {
            Title = "",
            Description = "New Description",
            Priority = "High"
        };
        var updateResponse = await client.PutAsJsonAsync($"/api/tickets/1", updateRequest);
        Assert.Equal(HttpStatusCode.BadRequest, updateResponse.StatusCode);
    }
    #endregion

    #region PatchTicket
    [Fact]
    public async Task PatchTicket_WithOnlyTitle_ShouldOnlyUpdateTitle()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        int ticketId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            var ticket = new Ticket(
                "Old Title",
                "Old Description",
                TicketPriority.Normal);
            await context.Tickets.AddAsync(ticket);
            await context.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var patchRequest = new PatchTicketRequest
        {
            Title = "New Title"
        };
        var patchResponse = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}", patchRequest);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var updatedTicket = await patchResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();
        Assert.NotNull(updatedTicket);
        Assert.Equal("New Title", updatedTicket.Title);
        Assert.Equal("Old Description", updatedTicket.Description);
        Assert.Equal("Normal", updatedTicket.Priority);
    }

    [Fact]
    public async Task PatchTicket_WithOnlyPriority_ShouldOnlyUpdatePriority()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        int ticketId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            var ticket = new Ticket(
                "Old Title",
                "Old Description",
                TicketPriority.Normal);
            await context.Tickets.AddAsync(ticket);
            await context.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var patchRequest = new PatchTicketRequest
        {
            Priority = "High"
        };
        var patchResponse = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}", patchRequest);
        Assert.Equal(HttpStatusCode.OK, patchResponse.StatusCode);
        var updatedTicket = await patchResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();
        Assert.NotNull(updatedTicket);
        Assert.Equal("Old Title", updatedTicket.Title);
        Assert.Equal("Old Description", updatedTicket.Description);
        Assert.Equal("High", updatedTicket.Priority);
    }

    [Fact]
    public async Task PatchTicket_WithUnknownTicket_ShouldReturnNotFound()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        var patchRequest = new PatchTicketRequest
        {
            Title = "New Title"
        };
        var patchResponse = await client.PatchAsJsonAsync($"/api/tickets/999", patchRequest);
        Assert.Equal(HttpStatusCode.NotFound, patchResponse.StatusCode);
    }

    [Fact]
    public async Task PatchTicket_WithInvalidPriority_ShouldReturnBadRequest()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        int ticketId;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            Ticket ticket = new
            (
                "Old Title",
                "Old Description",
                TicketPriority.Normal
            );
            await context.Tickets.AddAsync(ticket);
            await context.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var patchRequest = new PatchTicketRequest
        {
            Priority = "Invalid"
        };
        var patchResponse = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}", patchRequest);
        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
        var getResponse = await client.GetAsync($"/api/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var ticketDetails = await getResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();
        Assert.NotNull(ticketDetails);
        Assert.Equal("Old Title", ticketDetails.Title);
        Assert.Equal("Old Description", ticketDetails.Description);
        Assert.Equal("Normal", ticketDetails.Priority);
    }

    [Fact]
    public async Task PatchTicket_WithInvalidTitle_ShouldReturnBadRequest()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        int ticketId;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            Ticket ticket = new
            (
                "Old Title",
                "Old Description",
                TicketPriority.Normal
            );
            await context.Tickets.AddAsync(ticket);
            await context.SaveChangesAsync();
            ticketId = ticket.Id;
        }

        var patchRequest = new PatchTicketRequest
        {
            Title = ""
        };
        var patchResponse = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}", patchRequest);
        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var ticketDetails = await getResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();
        Assert.NotNull(ticketDetails);
        Assert.Equal("Old Title", ticketDetails.Title);
        Assert.Equal("Old Description", ticketDetails.Description);
        Assert.Equal("Normal", ticketDetails.Priority);
    }

    [Fact]
    public async Task PatchTicket_WithEmptyRequest_ShouldReturnBadRequest()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        int ticketId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            Ticket ticket = new
            (
                "Old Title",
                "Old Description",
                TicketPriority.Normal
            );
            await context.Tickets.AddAsync(ticket);
            await context.SaveChangesAsync();
            ticketId = ticket.Id;
        }
        var patchRequest = new PatchTicketRequest();
        var patchResponse = await client.PatchAsJsonAsync($"/api/tickets/{ticketId}", patchRequest);
        Assert.Equal(HttpStatusCode.BadRequest, patchResponse.StatusCode);
        var getResponse = await client.GetAsync($"/api/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var ticketDetails = await getResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();
        Assert.NotNull(ticketDetails);
        Assert.Equal("Old Title", ticketDetails.Title);
        Assert.Equal("Old Description", ticketDetails.Description);
        Assert.Equal("Normal", ticketDetails.Priority);
    }

    #endregion

    #region GetTickets
    #region Part 1
    private static async Task CreateNecessaryContextForGetTickets(HelpDeskDbContext context)
    {
        // Create tickets with different priorities and statuses
        await context.Tickets.AddAsync(new Ticket("Ticket Title 1", "Ticket Description 1", TicketPriority.High));
        await context.Tickets.AddAsync(new Ticket("Ticket Title 2", "Ticket Description 2", TicketPriority.High));
        await context.Tickets.AddAsync(new Ticket("Ticket Title 3", "Ticket Description 3", TicketPriority.High));
        await context.Tickets.AddAsync(new Ticket("Ticket Title 4", "Ticket Description 4", TicketPriority.High));
        await context.Tickets.AddAsync(new Ticket("Ticket Title 5", "Ticket Description 5", TicketPriority.Normal));
        await context.Tickets.AddAsync(new Ticket("Ticket Title 6", "Ticket Description 6", TicketPriority.High));

        // Create users with different roles
        await context.Users.AddAsync(new User("John", "Doe", "john.doe@example.com", UserRole.User));
        await context.Users.AddAsync(new User("Jane", "Smith", "jane.smith@example.com", UserRole.Technician));
        await context.Users.AddAsync(new User("Thomas", "DoesWhatHeCanLol", "thomas.doeswhathecanlol@example.com", UserRole.Administrator));
        await context.SaveChangesAsync();

        var ticket3 = context.Tickets.First(t => t.Title == "Ticket Title 3");
        var ticket4 = context.Tickets.First(t => t.Title == "Ticket Title 4");
        var ticket5 = context.Tickets.First(t => t.Title == "Ticket Title 5");
        var ticket6 = context.Tickets.First(t => t.Title == "Ticket Title 6");
        ticket6.AdvanceStatus(); // open -> in progress

        var user1 = context.Users.First(u => u.Firstname == "John");
        var user2 = context.Users.First(u => u.Firstname == "Jane");
        var user3 = context.Users.First(u => u.Firstname == "Thomas");

        // assign users
        // ticket1.AssignedUser is null
        // ticket2.AssignedUser is null
        ticket3.AssignUser(user1);
        ticket4.AssignUser(user1);
        ticket5.AssignUser(user2);
        ticket6.AssignUser(user3);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetTickets_WithValidRequest_ShouldReturnOk()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        HelpDeskDbContext? context = null;
        using (var scope = factory.Services.CreateScope())
        {
            context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForGetTickets(context);
        }

        var responsePage1 = await client.GetAsync($"/api/tickets?status=Open&priority=High&page=1&pageSize=3");
        Assert.Equal(HttpStatusCode.OK, responsePage1.StatusCode);
        var pagedResponse1 = await responsePage1.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(pagedResponse1);
        Assert.Equal(4, pagedResponse1.TotalCount);
        Assert.Equal(3, pagedResponse1.Items.Count);
        Assert.Equal(1, pagedResponse1.Page);
        Assert.Equal(3, pagedResponse1.PageSize);
        Assert.All(pagedResponse1.Items, item =>
        {
            Assert.Equal("High", item.Priority);
            Assert.Equal("Open", item.Status);
        });

        List<TicketListItemResponse> filteredTicketsList = [.. pagedResponse1.Items];
        var responsePage2 = await client.GetAsync($"/api/tickets?status=Open&priority=High&page=2&pageSize=3");
        Assert.Equal(HttpStatusCode.OK, responsePage2.StatusCode);
        var pagedResponse2 = await responsePage2.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(pagedResponse2);
        Assert.Equal(4, pagedResponse2.TotalCount);
        Assert.Single(pagedResponse2.Items);
        Assert.Equal(2, pagedResponse2.Page);
        Assert.Equal(3, pagedResponse2.PageSize);
        Assert.Equal("High", pagedResponse2.Items.First().Priority);
        Assert.Equal("Open", pagedResponse2.Items.First().Status);
        Assert.DoesNotContain(pagedResponse2.Items.First().Title, pagedResponse1.Items.Select(t => t.Title));
        filteredTicketsList.AddRange(pagedResponse2.Items);
        using (var scope = factory.Services.CreateScope())
        {
            context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            var expectedTickets = await context.Tickets
                .Where(t =>
                       t.Status == TicketStatus.Open
                      && t.Priority == TicketPriority.High)
                .OrderByDescending(t => t.CreationDate)
                .ThenByDescending(t => t.Id)
                .ToListAsync();


            Assert.Equal(4, expectedTickets.Count);
            for (int i = 0; i < expectedTickets.Count; i++)
            {
                Assert.Equal(expectedTickets[i].Title, filteredTicketsList[i].Title);
                Assert.Equal(
                    expectedTickets[i].Priority.ToString(),
                    filteredTicketsList[i].Priority);
                Assert.Equal(
                    expectedTickets[i].Status.ToString(),
                    filteredTicketsList[i].Status);
            }
        }
    }

    [Fact]
    public async Task GetTickets_WithInvalidStatus_ShouldReturnBadRequest()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/tickets?status=InvalidStatus");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTickets_WithInvalidPriority_ShouldReturnBadRequest()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/tickets?priority=InvalidPriority");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTickets_WithHasAssigneeFalseAndAssignedUserId_ShouldReturnBadRequest()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync($"/api/tickets?hasAssignee=false&assignedUserId=1");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTickets_WithTitleSearch_ShouldReturnFilteredTickets()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        HelpDeskDbContext? context = null;
        using (var scope = factory.Services.CreateScope())
        {
            context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForGetTickets(context);
        }
        var response = await client.GetAsync($"/api/tickets?search=Ticket Title 1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(pagedResponse);
        Assert.Single(pagedResponse.Items);
        Assert.Equal("Ticket Title 1", pagedResponse.Items.First().Title);

        var filteredTicket = pagedResponse.Items.First();
        using (var scope = factory.Services.CreateScope())
        {
            context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            var expectedTicket = await context.Tickets.Where(t => t.Title == "Ticket Title 1").ToListAsync();
            Assert.NotNull(expectedTicket);
            Assert.Single(expectedTicket);
            Assert.Equal(expectedTicket.First().Title, filteredTicket.Title);
            Assert.Equal(expectedTicket.First().Priority.ToString(), filteredTicket.Priority);
            Assert.Equal(expectedTicket.First().Status.ToString(), filteredTicket.Status);
        }
    }

    [Fact]
    public async Task GetTickets_WithDescriptionSearch_ShouldReturnFilteredTickets()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        HelpDeskDbContext? context = null;
        using (var scope = factory.Services.CreateScope())
        {
            context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForGetTickets(context);
        }
        var response = await client.GetAsync($"/api/tickets?search=Description 4");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(pagedResponse);
        Assert.Single(pagedResponse.Items);
        var filteredTicket = pagedResponse.Items.First();

        using (var scope = factory.Services.CreateScope())
        {
            context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            var expectedTicket = await context.Tickets.Where(t => t.Description == "Ticket Description 4").ToListAsync();
            Assert.NotNull(expectedTicket);
            Assert.Single(expectedTicket);
            Assert.Equal(expectedTicket.First().Title, filteredTicket.Title);
            Assert.Equal(expectedTicket.First().Priority.ToString(), filteredTicket.Priority);
            Assert.Equal(expectedTicket.First().Status.ToString(), filteredTicket.Status);
        }
    }

    [Fact]

    public async Task GetTickets_WithUserAssignedId_ShouldReturnFilteredTickets()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForGetTickets(context);
        }
        var response = await client.GetAsync($"/api/tickets?assignedUserId=1");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(pagedResponse);
        Assert.NotNull(pagedResponse.Items);

        var filteredTickets = pagedResponse.Items;
        List<Ticket>? expectedTickets = null;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            expectedTickets = await context.Tickets
                .Where(t => t.AssignedUser != null && t.AssignedUser.Id == 1).OrderByDescending(t => t.CreationDate).ThenByDescending(t => t.Id)
                .ToListAsync();
        }
        Assert.Equal(2, expectedTickets.Count);
        Assert.Equal(2, filteredTickets.Count);
        Assert.Equal(2, pagedResponse.TotalCount);

        int i = 0;
        foreach (TicketListItemResponse t in filteredTickets)
        {
            Assert.Equal(expectedTickets[i].Title, t.Title);
            Assert.Equal(expectedTickets[i].Priority.ToString(), t.Priority);
            Assert.Equal(expectedTickets[i].Status.ToString(), t.Status);
            i++;
        }
    }

    [Fact]
    public async Task GetTickets_WithUserAssignedIdNull_ShouldReturnAllTickets()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForGetTickets(context);
        }
        var response = await client.GetAsync($"/api/tickets?assignedUserId=");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(pagedResponse);
        Assert.Equal(6, pagedResponse.Items.Count);
    }

    [Fact]
    public async Task GetTickets_WhenHasAssigneeIsTrue_ShouldReturnFilteredTicketsWithAssignee()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForGetTickets(context);
        }
        var response = await client.GetAsync($"/api/tickets?hasAssignee=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(pagedResponse);
        var filteredTickets = pagedResponse.Items;

        List<Ticket>? expectedTickets = null;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            expectedTickets = await context.Tickets.Where(t => t.AssignedUser != null).OrderByDescending(t => t.CreationDate).ThenByDescending(t => t.Id).ToListAsync();
        }
        Assert.Equal(expectedTickets.Count, filteredTickets.Count);
        int i = 0;
        foreach (TicketListItemResponse t in filteredTickets)
        {
            Assert.Equal(expectedTickets[i].Title, t.Title);
            Assert.Equal(expectedTickets[i].Priority.ToString(), t.Priority);
            Assert.Equal(expectedTickets[i].Status.ToString(), t.Status);
            i++;
        }
    }

    [Fact]
    public async Task GetTickets_WhenHasAssigneeIsFalse_ShouldReturnFilteredTicketsWithoutAssignee()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForGetTickets(context);
        }
        var response = await client.GetAsync($"/api/tickets?hasAssignee=false");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(pagedResponse);
        var filteredTickets = pagedResponse.Items;

        List<Ticket>? expectedTickets = null;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            expectedTickets = await context.Tickets.Where(t => t.AssignedUser == null).OrderByDescending(t => t.CreationDate).ThenByDescending(t => t.Id).ToListAsync();
        }
        Assert.Equal(expectedTickets.Count, filteredTickets.Count);
        int i = 0;
        foreach (TicketListItemResponse t in filteredTickets)
        {
            Assert.Equal(expectedTickets[i].Title, t.Title);
            Assert.Equal(expectedTickets[i].Priority.ToString(), t.Priority);
            Assert.Equal(expectedTickets[i].Status.ToString(), t.Status);
            i++;
        }
    }

    [Fact]
    public async Task Get_Tickets_WithMultipleFilters_ShouldReturnFilteredTickets()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForGetTickets(context);
        }

        var response = await client.GetAsync("/api/tickets?status=open&priority=high&hasAssignee=true");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(pagedResponse);
        // expected tickets matchings filters : tickets 3 and 4
        Assert.Equal(2, pagedResponse.Items.Count);
        var filteredTickets = pagedResponse.Items;
        Assert.Equal(4, filteredTickets.First().Id);
        Assert.Equal(3, filteredTickets.Last().Id);
        Assert.All(filteredTickets, t =>
        {
            Assert.Equal("High", t.Priority);
            Assert.Equal("Open", t.Status);
        });
        List<Ticket>? expectedTickets = null;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            expectedTickets = await context.Tickets.Where(t => t.AssignedUser != null
                                                            && t.Priority == TicketPriority.High
                                                            && t.Status == TicketStatus.Open)
                                                    .OrderByDescending(t => t.CreationDate).ThenByDescending(t => t.Id).ToListAsync();
        }
        Assert.Equal(2, expectedTickets.Count);
        Assert.Equal(expectedTickets.First().Id, filteredTickets.First().Id);
        Assert.Equal(expectedTickets.Last().Id, filteredTickets.Last().Id);
    }

    [Fact]
    public async Task GetTickets_WithOOBPaging_ShouldReturnEmptyItems()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForGetTickets(context);
        }

        var response = await client.GetAsync("/api/tickets?pageSize=3&page=5");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(pagedResponse);
        Assert.NotNull(pagedResponse.Items);
        Assert.Empty(pagedResponse.Items);
        Assert.Equal(6, pagedResponse.TotalCount);
    }

    [Fact]
    public async Task GetTickets_WithInvalidPageRelatedRequest_ShouldReturnBadRequest()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        var response1 = await client.GetAsync("/api/tickets?page=0");
        Assert.Equal(HttpStatusCode.BadRequest, response1.StatusCode);
        var response2 = await client.GetAsync("/api/tickets?pageSize=101");
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
        var response3 = await client.GetAsync("/api/tickets?pageSize=0");
        Assert.Equal(HttpStatusCode.BadRequest, response3.StatusCode);
    }

    [Fact]
    public async Task GetTickets_WithNoMatchSearch_ShouldReturnEmptyItems()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<HelpDeskDbContext>();

            await CreateNecessaryContextForGetTickets(context);
        }

        var response = await client.GetAsync("/api/tickets?search=NOMATCH");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var pagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(pagedResponse);
        Assert.Empty(pagedResponse.Items);
        Assert.Equal(0, pagedResponse.TotalCount);
    }
    #endregion

    #region Part 2
    private static async Task CreateNecessaryContextForTicketsSortingTests(HelpDeskDbContext context)
    {
        // Create tickets with different priorities and statuses
        var ticket1 = new Ticket("Computer not booting", "Ticket Description 1", TicketPriority.Low);
        var ticket2 = new Ticket("Printer caught on fire (happens?)", "Ticket Description 2", TicketPriority.High);
        var ticket3 = new Ticket("Client sad after cat ran away", "Ticket Description 3", TicketPriority.High);
        var ticket4 = new Ticket("I have no more ideas for titles", "Ticket Description 4", TicketPriority.Low);
        var ticket5 = new Ticket("Just kidding here is one more", "Ticket Description 5", TicketPriority.Normal);
        var ticket6 = new Ticket("Monitor HDMI port not working", "Ticket Description 6", TicketPriority.High);
        await context.Tickets.AddRangeAsync([ticket1, ticket2, ticket3, ticket4, ticket5, ticket6]);
        await context.SaveChangesAsync();

        ticket1.AdvanceStatus(); // open -> in progress

        ticket3.AdvanceStatus(); // open -> in progress
        ticket3.AdvanceStatus(); // in progress -> resolved
        ticket3.AdvanceStatus(); // resolved -> closed

        ticket5.AdvanceStatus(); // open -> in progress
        ticket5.AdvanceStatus(); // in progress -> resolved

        ticket6.AdvanceStatus(); // open -> in progress

        await context.SaveChangesAsync();

        context.Entry(ticket1)
            .Property(t => t.CreationDate)
            .CurrentValue = new DateTime(2026, 1, 3, 0, 0, 0, DateTimeKind.Utc);

        context.Entry(ticket2)
            .Property(t => t.CreationDate)
            .CurrentValue = new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc);

        context.Entry(ticket3)
            .Property(t => t.CreationDate)
            .CurrentValue = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        context.Entry(ticket4)
            .Property(t => t.CreationDate)
            .CurrentValue = new DateTime(2025, 1, 7, 0, 0, 0, DateTimeKind.Utc);

        context.Entry(ticket5)
            .Property(t => t.CreationDate)
            .CurrentValue = new DateTime(2027, 1, 11, 0, 0, 0, DateTimeKind.Utc);

        context.Entry(ticket6)
            .Property(t => t.CreationDate)
            .CurrentValue = new DateTime(2012, 10, 28, 0, 0, 0, DateTimeKind.Utc);
        await context.SaveChangesAsync();

    }

    [Fact]
    public async Task GetSortedTickets_WithSortByAscTitle_ShouldReturnTicketsSortedByAscTitle()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForTicketsSortingTests(context);
        }

        var response = await client.GetAsync("/api/tickets?sortDirection=asc&sortBy=title");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sortedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(sortedResponse);
        Assert.NotNull(sortedResponse.Items);
        Assert.Equal(6, sortedResponse.Items.Count);
        var sortedResponseList = sortedResponse.Items.ToList();
        for (int i = 1; i < 6; i++)
        {
            int order = string.Compare(sortedResponseList[i - 1].Title, sortedResponseList[i].Title, StringComparison.Ordinal);
            Assert.True(order < 0);
        }
    }

    [Fact]
    public async Task GetSortedTickets_WithSortByDescTitle_ShouldReturnTicketsSortedByDescTitle()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForTicketsSortingTests(context);
        }

        var response = await client.GetAsync("/api/tickets?sortDirection=desc&sortBy=title");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sortedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(sortedResponse);
        Assert.NotNull(sortedResponse.Items);
        Assert.Equal(6, sortedResponse.Items.Count);
        var sortedResponseList = sortedResponse.Items.ToList();
        for (int i = 1; i < 6; i++)
        {
            int order = string.Compare(sortedResponseList[i - 1].Title, sortedResponseList[i].Title, StringComparison.Ordinal);
            Assert.True(order > 0);
        }
    }

    [Fact]
    public async Task GetSortedTickets_WithSortByAscPriority_ShouldReturnTicketsSortedByAscPriority()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForTicketsSortingTests(context);
        }

        var response = await client.GetAsync("/api/tickets?sortDirection=asc&sortBy=priority");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sortedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(sortedResponse);
        Assert.NotNull(sortedResponse.Items);
        Assert.Equal(6, sortedResponse.Items.Count);
        var sortedResponseList = sortedResponse.Items.ToList();
        for (int i = 1; i < 6; i++)
        {
            Assert.True(Enum.TryParse<TicketPriority>(sortedResponseList[i - 1].Priority, out var prio1));
            Assert.True(Enum.TryParse<TicketPriority>(sortedResponseList[i].Priority, out var prio2));
            Assert.True(prio1 <= prio2);
            if (prio1 == prio2)
            {
                int id1 = sortedResponseList[i - 1].Id;
                int id2 = sortedResponseList[i].Id;
                Assert.True(id1 < id2);
            }
        }
    }

    [Fact]
    public async Task GetSortedTickets_WithSortByDescStatus_ShouldReturnTicketsSortedByDescStatus()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForTicketsSortingTests(context);
        }

        var response = await client.GetAsync("/api/tickets?sortDirection=desc&sortBy=status");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sortedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(sortedResponse);
        Assert.NotNull(sortedResponse.Items);
        Assert.Equal(6, sortedResponse.Items.Count);
        var sortedResponseList = sortedResponse.Items.ToList();
        for (int i = 1; i < 6; i++)
        {

            Assert.True(Enum.TryParse<TicketStatus>(sortedResponseList[i - 1].Status, out var status1));
            Assert.True(Enum.TryParse<TicketStatus>(sortedResponseList[i].Status, out var status2));
            Assert.True(status1 >= status2);
            if (status1 == status2)
            {
                int id1 = sortedResponseList[i - 1].Id;
                int id2 = sortedResponseList[i].Id;
                Assert.True(id1 > id2);
            }
        }
    }

    [Fact]
    public async Task GetSortedTickets_WithNoSortOption_ShouldReturnTicketsSortedByDescCreationDate()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForTicketsSortingTests(context);
        }

        var response = await client.GetAsync("/api/tickets");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var sortedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<TicketListItemResponse>>();
        Assert.NotNull(sortedResponse);
        Assert.NotNull(sortedResponse.Items);
        Assert.Equal(6, sortedResponse.Items.Count);
        var sortedResponseList = sortedResponse.Items.ToList();

        List<int> expectedIds;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider
                .GetRequiredService<HelpDeskDbContext>();

            expectedIds = await context.Tickets
                .OrderByDescending(t => t.CreationDate)
                .ThenByDescending(t => t.Id)
                .Select(t => t.Id)
                .ToListAsync();
        }

        Assert.Equal(expectedIds, sortedResponse.Items.Select(t => t.Id));
    }

    [Fact]
    public async Task GetSortedTickets_WithInvalidSortBy_ShouldReturnBadRequest()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/tickets?sortBy=model");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetSortedTickets_WithInvalidSortDirection_ShouldReturnBadRequest()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/tickets?sortDirection=linear");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion
    #endregion

    #region GetComments

    private static async Task CreateNecessaryContextForGetComments(HelpDeskDbContext context)
    {
        // Create ticket to add comments to
        Ticket ticket1 = new("Ticket Title 1", "Ticket Description 1", TicketPriority.High);
        await context.Tickets.AddAsync(ticket1);

        // Create users with different roles
        User u1 = new("John", "Doe", "john.doe@example.com", UserRole.User);
        User u2 = new("Jane", "Smith", "jane.smith@example.com", UserRole.Technician);
        User u3 = new("Thomas", "DoesWhatHeCanLol", "thomas.doeswhathecanlol@example.com", UserRole.Administrator);
        await context.Users.AddRangeAsync([u1, u2, u3]);

        Comment c1 = new(u1, "The first comment (u1's first)");
        Comment c2 = new(u2, "The second comment (u2's first)");
        Comment c3 = new(u1, "The third comment (u1's second)");
        Comment c4 = new(u3, "The fourth comment (u3's first)");
        Comment c5 = new(u1, "The fifth comment (u1's third)");
        Comment c6 = new(u1, "The sixth comment (u1's fourth)");
        await context.SaveChangesAsync();

        context.Entry(c1)
            .Property(c => c.CreationDate)
            .CurrentValue = new DateTime(2005, 1, 3, 0, 0, 0, DateTimeKind.Utc);

        context.Entry(c2)
            .Property(c => c.CreationDate)
            .CurrentValue = new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc);

        context.Entry(c3)
            .Property(c => c.CreationDate)
            .CurrentValue = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        context.Entry(c4)
            .Property(c => c.CreationDate)
            .CurrentValue = new DateTime(2025, 1, 7, 0, 0, 0, DateTimeKind.Utc);

        context.Entry(c5)
            .Property(c => c.CreationDate)
            .CurrentValue = new DateTime(2026, 1, 6, 0, 0, 0, DateTimeKind.Utc);

        context.Entry(c6)
            .Property(t => t.CreationDate)
            .CurrentValue = new DateTime(2012, 10, 28, 0, 0, 0, DateTimeKind.Utc);

        ticket1.AddComment(c1);
        ticket1.AddComment(c2);
        ticket1.AddComment(c3);
        ticket1.AddComment(c4);
        ticket1.AddComment(c5);
        ticket1.AddComment(c6);
        await context.SaveChangesAsync();

    }

    [Fact]
    public async Task GetComments_WithValidRequest_ShouldReturnOk()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForGetComments(context);
        }

        var response = await client.GetAsync($"/api/tickets/1/comments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var commentPagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>();
        Assert.NotNull(commentPagedResponse);
        Assert.Equal(6, commentPagedResponse.TotalCount);
        Assert.NotNull(commentPagedResponse.Items);
        Assert.Equal(6, commentPagedResponse.Items.Count);
        var commentsList = commentPagedResponse.Items.ToList();

        var janeComment = commentsList.Single(
            c => c.Content == "The second comment (u2's first)");

        Assert.Equal("Jane", janeComment.Author.Firstname);
        Assert.Equal("Smith", janeComment.Author.Lastname);

        for (int i = 1; i < 6; i++)
        {
            var creationDate1 = commentsList[i - 1].CreationDate;
            var creationDate2 = commentsList[i].CreationDate;
            Assert.True(creationDate1 >= creationDate2);
            if (creationDate1 == creationDate2)
            {
                int id1 = commentsList[i - 1].Id;
                int id2 = commentsList[i].Id;
                Assert.True(id1 > id2);
            }
        }
    }

    [Fact]
    public async Task GetComments_WithPagedDataRequest_ShouldReturnOkAndProperlyPagedItems()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await CreateNecessaryContextForGetComments(context);
        }

        var response1 = await client.GetAsync($"/api/tickets/1/comments?pageSize=4&page=1");
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);
        var commentPagedResponse1 = await response1.Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>();
        Assert.NotNull(commentPagedResponse1);
        Assert.Equal(6, commentPagedResponse1.TotalCount);
        Assert.NotNull(commentPagedResponse1.Items);
        Assert.Equal(4, commentPagedResponse1.Items.Count);

        var response2 = await client.GetAsync($"/api/tickets/1/comments?pageSize=4&page=2");
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var commentPagedResponse2 = await response2.Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>();
        Assert.NotNull(commentPagedResponse2);
        Assert.Equal(6, commentPagedResponse2.TotalCount);
        Assert.NotNull(commentPagedResponse2.Items);
        Assert.Equal(2, commentPagedResponse2.Items.Count);
        Assert.DoesNotContain(commentPagedResponse2.Items, c2 => commentPagedResponse1.Items.Any(c1 => c1.Id == c2.Id));

        var response3 = await client.GetAsync($"/api/tickets/1/comments?pageSize=4&page=3");
        Assert.Equal(HttpStatusCode.OK, response3.StatusCode);
        var commentPagedResponse3 = await response3.Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>();
        Assert.NotNull(commentPagedResponse3);
        Assert.Equal(6, commentPagedResponse3.TotalCount);
        Assert.NotNull(commentPagedResponse3.Items);
        Assert.Empty(commentPagedResponse3.Items);
    }

    [Fact]
    public async Task GetComments_WhenTicketDoesNotExists_ShouldReturnNotFound()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync($"/api/tickets/1/comments");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.NotNull(problem);
        Assert.Equal(404, problem.Status);
    }

    [Fact]
    public async Task GetComments_WhenTicketDoesNotContainComments_ShouldReturnOkAndEmptyItems()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            await context.Tickets.AddAsync(new Ticket("Some title", "Some description", TicketPriority.High));
            await context.SaveChangesAsync();
        }

        var response = await client.GetAsync($"/api/tickets/1/comments");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var commentPagedResponse = await response.Content.ReadFromJsonAsync<PagedResponse<CommentResponse>>();
        Assert.NotNull(commentPagedResponse);
        Assert.Empty(commentPagedResponse.Items);
        Assert.Equal(0, commentPagedResponse.TotalCount);
        Assert.NotNull(commentPagedResponse.Items);
    }

    [Theory]
    [InlineData("/api/tickets/1/comments?page=0")]
    [InlineData("/api/tickets/1/comments?pageSize=0")]
    [InlineData("/api/tickets/1/comments?pageSize=101")]
    public async Task GetComments_WithInvalidPagination_ShouldReturnBadRequest(
    string url)
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
    #endregion
}
