using HelpDesk.Application.Services;
using HelpDesk.Domain;
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
            var service = new TicketCreationService(ticketRepository, userRepository);

            const string title = "Keyboard broken";
            const string description = "Several keys do not work";
            const TicketPriority priority = TicketPriority.High;

            User requester = new("Requester", "Lastname", "requester@example.com", UserRole.Technician);
            await userRepository.AddAsync(requester);
            await userRepository.SaveChangesAsync();

            var ticket = await service.CreateAsync(title, description, priority, requester.Id);
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
            var service = new TicketCreationService(ticketRepository, userRepository);

            User requester = new("Requester", "Lastname", "requester@example.com", UserRole.Technician);
            await userRepository.AddAsync(requester);
            await userRepository.SaveChangesAsync();

            var invalidPriority = (TicketPriority)999;

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateAsync("Test", "Description", invalidPriority, requester.Id));

            Assert.False(ticketRepository.SaveChangesCalled);
            Assert.Equal(0, ticketRepository.Count());
        }

        [Fact]
        public async Task CreateAsync_WithUnknownRequester_ShouldNotCreateTicket()
        {
            var ticketRepository = new FakeTicketRepository();
            var userRepository = new FakeUserRepository();
            var service = new TicketCreationService(ticketRepository, userRepository);

            const string title = "Keyboard broken";
            const string description = "Several keys do not work";
            const TicketPriority priority = TicketPriority.High;
            await Assert.ThrowsAsync<KeyNotFoundException>(() =>
                service.CreateAsync(title, description, priority, 999));
            Assert.Equal(0, ticketRepository.Count());
        }
    }
}
