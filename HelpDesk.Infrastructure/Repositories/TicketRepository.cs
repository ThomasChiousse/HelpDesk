using HelpDesk.Application.Common.Pagination;
using HelpDesk.Application.Repositories;
using HelpDesk.Application.Sorting;
using HelpDesk.Application.Tickets.Queries;
using HelpDesk.Domain;
using HelpDesk.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HelpDesk.Infrastructure.Repositories;

public class TicketRepository : ITicketRepository
{
    private readonly HelpDeskDbContext _context;

    public TicketRepository(HelpDeskDbContext context)
    {
        _context = context;
    }



    public async Task AddAsync(Ticket ticket, CancellationToken cancellationToken = default)
    {
        await _context.Tickets.AddAsync(ticket, cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Ticket?> GetByIdAsync(
      int id,
      CancellationToken cancellationToken = default)
    {
        return await _context.Tickets.FirstOrDefaultAsync(
        ticket => ticket.Id == id,
        cancellationToken);
    }

    public async Task<Ticket?> GetByIdWithDetailsAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets.AsNoTracking().Include(t => t.AssignedUser)
            .Include(t => t.Comments).ThenInclude(c => c.Author)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<Ticket?> GetByIdWithAssigneeAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets.Include(t => t.AssignedUser).FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    public async Task<PagedResult<TicketListItem>> GetPagedAsync(TicketQueryOptions options, CancellationToken cancellationToken = default)
    {
        IQueryable<Ticket> query = _context.Tickets.AsNoTracking();
        if (options.Status.HasValue)
        {
            query = query.Where(t => t.Status == options.Status.Value);
        }
        if (options.Priority.HasValue)
        {
            query = query.Where(t => t.Priority == options.Priority.Value);
        }

        if (options.HasAssignee.HasValue)
        {
            if (options.HasAssignee.Value)
            {
                query = options.AssignedUserId is not null ?
                    query.Where(t => t.AssignedUser != null && t.AssignedUser.Id == options.AssignedUserId) :
                    query.Where(t => t.AssignedUser != null);
            }
            else
            {
                query = query.Where(t => t.AssignedUser == null);
            }
        }
        else
        {
            query = options.AssignedUserId is not null ?
            query.Where(t => t.AssignedUser != null && t.AssignedUser.Id == options.AssignedUserId) :
            query;
        }


        if (!string.IsNullOrWhiteSpace(options.Search))
        {
            query = query.Where(t => t.Title.Contains(options.Search) || t.Description.Contains(options.Search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        IOrderedQueryable<Ticket> orderedQuery =
           (options.SortField, options.SortDirection) switch
           {
               (TicketSortField.CreationDate, SortDirection.Ascending) => query.OrderBy(t => t.CreationDate).ThenBy(t => t.Id),
               (TicketSortField.CreationDate, SortDirection.Descending) => query.OrderByDescending(t => t.CreationDate).ThenByDescending(t => t.Id),
               (TicketSortField.Title, SortDirection.Ascending) => query.OrderBy(t => t.Title).ThenBy(t => t.Id),
               (TicketSortField.Title, SortDirection.Descending) => query.OrderByDescending(t => t.Title).ThenByDescending(t => t.Id),
               (TicketSortField.Priority, SortDirection.Ascending) => query.OrderBy(t => t.Priority).ThenBy(t => t.Id),
               (TicketSortField.Priority, SortDirection.Descending) => query.OrderByDescending(t => t.Priority).ThenByDescending(t => t.Id),
               (TicketSortField.Status, SortDirection.Ascending) => query.OrderBy(t => t.Status).ThenBy(t => t.Id),
               (TicketSortField.Status, SortDirection.Descending) => query.OrderByDescending(t => t.Status).ThenByDescending(t => t.Id),
               _ => throw new ArgumentOutOfRangeException()
           };

        var items = await orderedQuery.Skip((options.Page - 1) * options.PageSize).Take(options.PageSize)
            .Select(t => new TicketListItem(t.Id, t.Title, t.Priority, t.Status, t.CreationDate))
            .ToListAsync(cancellationToken);
        return new PagedResult<TicketListItem>(items, totalCount);
    }

    public async Task<bool> ExistsAsync(int ticketId, CancellationToken cancellationToken = default)
    {
        return await _context.Tickets
            .AnyAsync(t => t.Id == ticketId, cancellationToken);
    }

    public async Task<PagedResult<CommentListItem>> GetCommentsPagedAsync(int ticketId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        IQueryable<Comment> query = _context.Tickets.Where(t => t.Id == ticketId)
                .SelectMany(t => t.Comments);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderByDescending(c => c.CreationDate)
            .ThenByDescending(c => c.Id)
            .Select(c => new CommentListItem(
                    c.Id,
                    c.Content,
                    c.CreationDate,
                    new CommentAuthorItem(
                        c.Author.Id,
                        c.Author.Firstname,
                        c.Author.Lastname)
                ))
            .ToListAsync(cancellationToken);

        return new PagedResult<CommentListItem>(items, totalCount);
    }
}