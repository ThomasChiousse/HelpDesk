using HelpDesk.Application.Services.Tickets;
using HelpDesk.Domain;
using HelpDesk.Tests.Helpers.Authentication;
using HelpDesk.Tests.TestRepositories;

namespace HelpDesk.Tests.ServicesTests
{
    public class TicketCreationServiceTests
    {
        [Fact]
        public async Task CreateAsync_WithValidData_ShouldCreateTicket()
        {
            var ticketRepository = new FakeTicketRepository();
            var userRepository = new FakeUserRepository();

            User requester = new("Requester", "Lastname", "requester@example.com", UserRole.Technician);
            await userRepository.AddAsync(requester);
            await userRepository.SaveChangesAsync();
            var currentUser = new FakeCurrentUser
            {
                Id = requester.Id,
                Role = requester.Role
            };
            var service = new TicketCreationService(ticketRepository, userRepository, currentUser);

            const string title = "Keyboard broken";
            const string description = "Several keys do not work";
            const TicketPriority priority = TicketPriority.High;

            var ticket = await service.CreateAsync(title, description, priority);
            // Assert
            Assert.Equal(title, ticket.Title);
            Assert.Equal(description, ticket.Description);
            Assert.Equal(priority, ticket.Priority);
            Assert.Equal(TicketStatus.Open, ticket.Status);

            Assert.True(ticketRepository.Contains(ticket));
            Assert.True(ticketRepository.SaveChangesCalled);

            Assert.NotNull(ticket.Requester);
            Assert.Equal(requester.Id, ticket.Requester.Id);
        }

        [Fact]
        public async Task CreateAsync_WithInvalidPriority_ShouldThrowArgumentException()
        {
            // Arrange
            var ticketRepository = new FakeTicketRepository();
            var userRepository = new FakeUserRepository();

            User requester = new("Requester", "Lastname", "requester@example.com", UserRole.Technician);
            await userRepository.AddAsync(requester);
            await userRepository.SaveChangesAsync();
            var currentUser = new FakeCurrentUser
            {
                Id = requester.Id,
                Role = requester.Role
            };
            var service = new TicketCreationService(ticketRepository, userRepository, currentUser);

            var invalidPriority = (TicketPriority)999;

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateAsync("Test", "Description", invalidPriority));

            Assert.False(ticketRepository.SaveChangesCalled);
            Assert.Equal(0, ticketRepository.Count());
        }

        [Fact]
        public async Task CreateAsync_WithUnknownRequester_ShouldNotCreateTicket()
        {
            var ticketRepository = new FakeTicketRepository();
            var userRepository = new FakeUserRepository();

            var currentUser = new FakeCurrentUser
            {
                Id = 999,
                Role = UserRole.User
            };
            var service = new TicketCreationService(ticketRepository, userRepository, currentUser);

            const string title = "Keyboard broken";
            const string description = "Several keys do not work";
            const TicketPriority priority = TicketPriority.High;
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateAsync(title, description, priority));
            Assert.Equal(0, ticketRepository.Count());
        }
    }
}
