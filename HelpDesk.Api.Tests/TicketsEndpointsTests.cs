using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Domain;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace HelpDesk.Api.Tests;

public class TicketsEndpointsTests
{
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
    public async Task AddComment_WithValidRequest_ShouldCreateComment()
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
        Assert.Equal(HttpStatusCode.Created, postResponse.StatusCode);

        var createdComment = await postResponse.Content.ReadFromJsonAsync<CommentResponse>();
        Assert.NotNull(createdComment);
        Assert.True(createdComment.Id > 0);
        Assert.Equal(userId, createdComment.Author.Id);
        Assert.Equal("This is a comment", createdComment.Content);

        var getResponse = await client.GetAsync($"/api/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var ticketFromDb = await getResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();
        Assert.NotNull(ticketFromDb);
        Assert.NotNull(ticketFromDb.Comments);
        var persistedComment = Assert.Single(ticketFromDb.Comments);
        Assert.Equal(createdComment.Id, persistedComment.Id);
        Assert.Equal(userId, persistedComment.Author.Id);
        Assert.Equal("This is a comment", ticketFromDb.Comments.First().Content);
    }

    [Fact]
    public async Task AddComment_WithUnknownTicket_ShouldReturnNotFound()
    {
        using var factory = new HelpDeskApiFactory();
        var client = factory.CreateClient();

        int userId;

        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<HelpDeskDbContext>();
            var user = new User("Thomas", "Banana", "email@example.com", UserRole.Technician);
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            userId = user.Id;
        }

        var request = new
        {
            AuthorId = userId,
            Content = "This is a comment"
        };
        var postResponse = await client.PostAsJsonAsync($"/api/tickets/999/comments", request);
        Assert.Equal(HttpStatusCode.NotFound, postResponse.StatusCode);
    }

    [Fact]
    public async Task AddComment_WithUnknownAuthor_ShouldReturnNotFound()
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

        var request = new
        {
            AuthorId = 999,
            Content = "This is a comment"
        };
        var postResponse = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/comments", request);
        Assert.Equal(HttpStatusCode.NotFound, postResponse.StatusCode);
    }

    [Fact]
    public async Task AddComment_ToClosedTicket_ShouldReturnConflict()
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

            ticket.AdvanceStatus(); // open -> in progress
            ticket.AdvanceStatus(); // in progress -> resolved
            ticket.AdvanceStatus(); // resolved -> closed

            var user = new User("Thomas", "Banana", "email@example.com", UserRole.Technician);

            await context.Tickets.AddAsync(ticket);
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            userId = user.Id;
            ticketId = ticket.Id;
        }

        var request = new
        {
            AuthorId = userId,
            Content = "This is a comment"
        };
        var postResponse = await client.PostAsJsonAsync($"/api/tickets/{ticketId}/comments", request);
        Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
        var getResponse = await client.GetAsync($"/api/tickets/{ticketId}");
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var ticketFromDb = await getResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();
        Assert.NotNull(ticketFromDb);
        Assert.Empty(ticketFromDb.Comments);
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
}
