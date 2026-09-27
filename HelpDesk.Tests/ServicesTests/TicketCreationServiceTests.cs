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

            var ticket = await service.CreateAsync(title, description, priority);
            // Assert
            Assert.Equal(title, ticket.Title);
            Assert.Equal(description, ticket.Description);
            Assert.Equal(priority, ticket.Priority);
            Assert.Equal(TicketStatus.Open, ticket.Status);

            Assert.True(ticketRepository.Contains(ticket));
            Assert.True(ticketRepository.SaveChangesCalled);
        }

        [Fact]
        public async Task CreateAsync_WithInvalidPriority_ShouldThrowArgumentException()
        {
            // Arrange
            var ticketRepository = new FakeTicketRepository();
            var userRepository = new FakeUserRepository();
            var service = new TicketCreationService(ticketRepository, userRepository);

            var invalidPriority = (TicketPriority)999;

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateAsync(
                    "Test",
                    "Description",
                    invalidPriority));

            Assert.False(ticketRepository.SaveChangesCalled);
            Assert.Equal(0, ticketRepository.Count());
        }
    }
}
