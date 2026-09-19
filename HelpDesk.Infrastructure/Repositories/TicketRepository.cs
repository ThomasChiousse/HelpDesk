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

    public async Task<(IReadOnlyCollection<TicketListItem> Items, int TotalCount)> GetPagedAsync(TicketStatus? status, TicketPriority? priority, int? assignedUserId, bool? hasAssignee,
        string? search, int page, int pageSize, TicketSortField sortField, SortDirection sortDirection, CancellationToken cancellationToken = default)
    {


        IQueryable<Ticket> query = _context.Tickets.AsNoTracking();
        if (status.HasValue)
        {
            query = query.Where(t => t.Status == status.Value);
        }
        if (priority.HasValue)
        {
            query = query.Where(t => t.Priority == priority.Value);
        }

        if (hasAssignee.HasValue)
        {
            if (hasAssignee.Value)
            {
                query = assignedUserId is not null ?
                    query.Where(t => t.AssignedUser != null && t.AssignedUser.Id == assignedUserId) :
                    query.Where(t => t.AssignedUser != null);
            }
            else
            {
                query = query.Where(t => t.AssignedUser == null);
            }
        }
        else
        {
            query = assignedUserId is not null ?
            query.Where(t => t.AssignedUser != null && t.AssignedUser.Id == assignedUserId) :
            query;
        }


        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(t => t.Title.Contains(search) || t.Description.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        IOrderedQueryable<Ticket> orderedQuery =
           (sortField, sortDirection) switch
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

        var items = await orderedQuery.Skip((page - 1) * pageSize).Take(pageSize).
            Select(t => new TicketListItem(t.Id, t.Title, t.Priority, t.Status, t.CreationDate)).
            ToListAsync(cancellationToken);
        return (items, totalCount);

    }
}