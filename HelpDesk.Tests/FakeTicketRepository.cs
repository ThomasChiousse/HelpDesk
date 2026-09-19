using HelpDesk.Application.Common.Pagination;
using HelpDesk.Application.Repositories;
using HelpDesk.Application.Tickets.Queries;
using HelpDesk.Domain;

namespace HelpDesk.Tests
{
    internal class FakeTicketRepository : ITicketRepository
    {
        private readonly List<Ticket> _tickets = [];
        public bool SaveChangesCalled { get; private set; }

        public void Add(Ticket ticket)
        {
            _tickets.Add(ticket);
        }

        public Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default)
        {
            _tickets.Add(ticket);
            return Task.CompletedTask;
        }

        public Task<Ticket?> GetByIdAsync(
            int id,
            CancellationToken cancellationToken = default)
        {
            var ticket = _tickets.FirstOrDefault(t => t.Id == id);

            return Task.FromResult(ticket);
        }

        public Task<Ticket?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_tickets.FirstOrDefault(t => t.Id == id));
        }

        public Task SaveChangesAsync(
            CancellationToken cancellationToken = default)
        {
            SaveChangesCalled = true;
            return Task.CompletedTask;
        }

        public bool Contains(Ticket ticket)
        {
            return _tickets.Contains(ticket);
        }

        public int Count() => _tickets.Count;

        public Task<Ticket?> GetByIdWithAssigneeAsync(int id, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<PagedResult<TicketListItem>> GetPagedAsync(TicketQueryOptions options, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }
}
