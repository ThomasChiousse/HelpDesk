using HelpDesk.Application.Repositories;
using HelpDesk.Domain;

namespace HelpDesk.Tests
{
    internal class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];
        public bool SaveChangesCalled { get; private set; }

        public void Add(User user) => _users.Add(user);

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<User?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var result = _users.FirstOrDefault(u => u.Id == id);
            return Task.FromResult(result);
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            var result = _users.FirstOrDefault(u => u.Email == email);
            return Task.FromResult(result);
        }
    }
}
