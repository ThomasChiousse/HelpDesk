using HelpDesk.Application.Repositories;
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
}