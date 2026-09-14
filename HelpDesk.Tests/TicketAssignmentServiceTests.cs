using HelpDesk.Application.Services;
using HelpDesk.Domain;

namespace HelpDesk.Tests
{
    public class TicketAssignmentServiceTests
    {
        private readonly FakeTicketRepository _ticketRepository = new();
        private readonly FakeUserRepository _userRepository = new();

        private static User CreateUser(int id = 12)
        {
            return new User(id, "Thomas", "Banana", "email@domain.com", UserRole.Technician);
        }
        private static Ticket CreateTicket(int id = 1)
        {
            return new Ticket(id,
                "Printer broken",
                "The printer doesn't work",
                TicketPriority.Normal);
        }

        [Fact]
        public async Task AssignUserAsync_WithExistingTicketAndUser_ShouldAssignUser()
        {
            // Arrange
            var ticket = CreateTicket(id: 11);
            var user = CreateUser();

            _ticketRepository.Add(ticket);
            _userRepository.Add(user);

            var service = new TicketAssignmentService(
                _ticketRepository,
                _userRepository);

            // Act
            await service.AssignUserAsync(ticket.Id, user.Id, CancellationToken.None);


            // Assert
            Assert.Same(user, ticket.AssignedUser);
            Assert.True(_ticketRepository.SaveChangesCalled);
        }

        [Fact]
        public async Task AssignUserAsync_WithUnknownTicket_ShouldThrowKeyNotFoundException()
        {
            var user = CreateUser();
            _userRepository.Add(user);
            var service = new TicketAssignmentService(_ticketRepository, _userRepository);

            await Assert.ThrowsAsync<KeyNotFoundException>(() => service.AssignUserAsync(1, user.Id, CancellationToken.None));
            Assert.False(_ticketRepository.SaveChangesCalled);
        }

        [Fact]
        public async Task AssignUserAsync_WithUnknownUser_ShouldThrowKeyNotFoundException()
        {

            var ticket = CreateTicket();
            _ticketRepository.Add(ticket);

            var service = new TicketAssignmentService(
                _ticketRepository,
                _userRepository);


            await Assert.ThrowsAsync<KeyNotFoundException>(
                () => service.AssignUserAsync(ticket.Id, 999));

            Assert.Null(ticket.AssignedUser);
            Assert.False(_ticketRepository.SaveChangesCalled);
        }

    }
}
