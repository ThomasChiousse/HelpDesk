using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Domain;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
        await context.Tickets.AddAsync(new Ticket("Computer not booting", "Ticket Description 1", TicketPriority.Low));
        await Task.Delay(TimeSpan.FromSeconds(1));
        await context.Tickets.AddAsync(new Ticket("Printer caught on fire (happens?)", "Ticket Description 2", TicketPriority.High));
        await context.Tickets.AddAsync(new Ticket("Client sad after cat ran away", "Ticket Description 3", TicketPriority.High));
        await Task.Delay(TimeSpan.FromSeconds(1));
        await context.Tickets.AddAsync(new Ticket("I have no more ideas for titles", "Ticket Description 4", TicketPriority.Low));
        await context.Tickets.AddAsync(new Ticket("Just kidding here is one more", "Ticket Description 5", TicketPriority.Normal));
        await Task.Delay(TimeSpan.FromSeconds(1));
        await context.Tickets.AddAsync(new Ticket("Monitor HDMI port not working", "Ticket Description 6", TicketPriority.High));

        await context.SaveChangesAsync();
        var ticket1 = context.Tickets.First(t => t.Id == 1);
        ticket1.AdvanceStatus(); // open -> in progress

        var ticket3 = context.Tickets.First(t => t.Id == 3);
        ticket3.AdvanceStatus(); // open -> in progress
        ticket3.AdvanceStatus(); // in progress -> resolved
        ticket3.AdvanceStatus(); // resolved -> closed

        var ticket5 = context.Tickets.First(t => t.Id == 5);
        ticket5.AdvanceStatus(); // open -> in progress
        ticket5.AdvanceStatus(); // in progress -> resolved

        var ticket6 = context.Tickets.First(t => t.Id == 6);
        ticket6.AdvanceStatus(); // open -> in progress

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
            Enum.TryParse<TicketPriority>(sortedResponseList[i - 1].Priority, out var prio1);
            Enum.TryParse<TicketPriority>(sortedResponseList[i].Priority, out var prio2);
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

            Enum.TryParse<TicketStatus>(sortedResponseList[i - 1].Status, out var status1);
            Enum.TryParse<TicketStatus>(sortedResponseList[i].Status, out var status2);
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
        for (int i = 1; i < 6; i++)
        {
            Assert.True(sortedResponseList[i - 1].CreationDate >= sortedResponseList[i].CreationDate);
        }
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
}
