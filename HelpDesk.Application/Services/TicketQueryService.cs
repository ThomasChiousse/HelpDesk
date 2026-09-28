using HelpDesk.Application.Authentication;
using HelpDesk.Application.Common.Pagination;
using HelpDesk.Application.Repositories;
using HelpDesk.Application.Tickets.Queries;
using HelpDesk.Domain;

namespace HelpDesk.Application.Services
{

    public class TicketQueryService
    {
        private readonly ITicketRepository _ticketRepository;
        private readonly ICurrentUser _currentUser;
        public TicketQueryService(ITicketRepository ticketRepository, ICurrentUser currentUser)
        {
            _ticketRepository = ticketRepository;
            _currentUser = currentUser;
        }

        public async Task<Ticket> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            var ticket = await _ticketRepository.GetByIdWithDetailsAsync(
                id,
                cancellationToken);

            return ticket is null
                ? throw new KeyNotFoundException(
                    $"Cannot find the ticket with id: {id}")
                : ticket;
        }

        public async Task<PagedResult<TicketListItem>> GetPagedAsync(TicketQueryOptions options, CancellationToken cancellationToken = default)
        {

            if (_currentUser.Role == UserRole.User)
            {
                options = options with
                {
                    RequesterId = _currentUser.Id
                };
            }
            return await _ticketRepository.GetPagedAsync(options, cancellationToken);
        }

        public async Task<PagedResult<CommentListItem>> GetCommentsPagedAsync(int ticketId, int page, int pageSize, CancellationToken cancellationToken = default)
        {
            if (!await _ticketRepository.ExistsAsync(ticketId, cancellationToken))
            {
                throw new KeyNotFoundException(
                    $"Cannot find the ticket with id: {ticketId}");
            }

            return await _ticketRepository.GetCommentsPagedAsync(
                ticketId,
                page,
                pageSize,
                cancellationToken);
        }

    }
}
