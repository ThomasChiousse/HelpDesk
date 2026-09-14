using HelpDesk.Api.Contracts.Tickets;
using HelpDesk.Domain;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace HelpDesk.Api.Tests;

public class TicketsEndpointsTests
{
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

            await context.Tickets.AddAsync(ticket);
            await context.Users.AddAsync(user);
            await context.SaveChangesAsync();
            ticketId = ticket.Id;
            userId = user.Id;

            var putResponse = await client.PutAsync($"/api/tickets/{ticketId}/assignee/{userId}", null);
            // not checking if the put worked because we have another test for this
        }

        var deleteResponse = await client.DeleteAsync($"api/tickets/{ticketId}/assignee/{userId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await client.GetAsync($"/api/tickets/{ticketId}");
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

            await context.Tickets.AddAsync(ticket);
            await context.Users.AddAsync(user1);
            await context.Users.AddAsync(user2);
            await context.SaveChangesAsync();

            ticketId = ticket.Id;
            user1Id = user1.Id;
            user2Id = user2.Id;

            var putResponse = await client.PutAsync($"/api/tickets/{ticketId}/assignee/{user1Id}", null);
        }

        var deleteResponse = await client.DeleteAsync($"api/tickets/{ticketId}/assignee/{user2Id}");
        Assert.Equal(HttpStatusCode.Conflict, deleteResponse.StatusCode);
        var getResponse = await client.GetAsync($"/api/tickets/{ticketId}");
        var ticketFromDb = await getResponse.Content.ReadFromJsonAsync<TicketDetailsResponse>();
        Assert.NotNull(ticketFromDb);
        Assert.NotNull(ticketFromDb.AssignedUser);
        Assert.Equal(user1Id, ticketFromDb.AssignedUser.Id);
    }

    [Fact]
    public async Task UnassignUser_WithUnknownTicket_ShouldReturnNotFound()
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

        var deleteResponse = await client.DeleteAsync($"api/tickets/999/assignee/{userId}");
        Assert.Equal(HttpStatusCode.NotFound, deleteResponse.StatusCode);
    }
}
