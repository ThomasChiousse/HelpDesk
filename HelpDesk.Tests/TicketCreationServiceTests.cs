using HelpDesk.Application.Services;
using HelpDesk.Domain;

namespace HelpDesk.Tests
{
    public class TicketCreationServiceTests
    {
        [Fact]
        public async Task CreateAsync_WithValidData_ShouldCreateTicket()
        {
            var repository = new FakeTicketRepository();

            var service = new TicketCreationService(repository);

            const string title = "Keyboard broken";
            const string description = "Several keys do not work";
            const TicketPriority priority = TicketPriority.High;

            var ticket = await service.CreateAsync(title, description, priority);
            // Assert
            Assert.Equal(title, ticket.Title);
            Assert.Equal(description, ticket.Description);
            Assert.Equal(priority, ticket.Priority);
            Assert.Equal(TicketStatus.Open, ticket.Status);

            Assert.True(repository.Contains(ticket));
            Assert.True(repository.SaveChangesCalled);
        }

        [Fact]
        public async Task CreateAsync_WithInvalidPriority_ShouldThrowArgumentException()
        {
            // Arrange
            var repository = new FakeTicketRepository();
            var service = new TicketCreationService(repository);

            var invalidPriority = (TicketPriority)999;

            // Act + Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CreateAsync(
                    "Test",
                    "Description",
                    invalidPriority));

            Assert.False(repository.SaveChangesCalled);
            Assert.Equal(0, repository.Count());
        }
    }
}
