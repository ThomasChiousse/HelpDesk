using HelpDesk.Domain;
using HelpDesk.Infrastructure.Persistence;
using HelpDesk.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;


var options = new DbContextOptionsBuilder<HelpDeskDbContext>()
    .UseSqlServer(
        @"Server=(localdb)\MSSQLLocalDB;
          Database=HelpDeskDb;
          Trusted_Connection=True;
          TrustServerCertificate=True;")
    .Options;

await using var context = new HelpDeskDbContext(options);
await using var context2 = new HelpDeskDbContext(options);
var user_repository = new UserRepository(context);
var ticket_repository = new TicketRepository(context);
var ticket_repository2 = new TicketRepository(context2);
var ticket = await ticket_repository.GetByIdWithDetailsAsync(1);
if (ticket is null) return;

ticket!.SetTitle("THIS SHOULD NOT BE SAVED");

await ticket_repository.SaveChangesAsync();

var ticket2 = await ticket_repository2.GetByIdAsync(1);
ticket2.SetTitle("THIS SHOULD BE SAVED");
await ticket_repository2.SaveChangesAsync();
Console.WriteLine(ticket2.Title);

async void Part1()
{
    var user = new User(
    "Tom",
    "Bana",
    "tom@helpdesk.local",
    UserRole.User);

    Console.WriteLine($"Before: {user.Id}");

    await user_repository.AddAsync(user);
    await user_repository.SaveChangesAsync();

    Console.WriteLine($"After: {user.Id}");

    Ticket ticket = new("Broken printer", "Printer not working anymore", TicketPriority.High, user);

    await ticket_repository.AddAsync(ticket);
    await ticket_repository.SaveChangesAsync();
}

async void Part2()
{
    var ticket = await context.Tickets
    .FirstOrDefaultAsync(t => t.Id == 1);


    var ticket2 = await context.Tickets
    .Include(t => t.AssignedUser)
    .Include(t => t.Comments)
        .ThenInclude(c => c.Author)
    .FirstOrDefaultAsync(t => t.Id == 1);

    Console.WriteLine(ticket?.Title);
    Console.WriteLine(ticket?.AssignedUser?.Firstname);
    Console.WriteLine(ticket?.Comments.Count);
}

async void Part3()
{
    var ticket = await context.Tickets
    .Include(t => t.AssignedUser)
    .Include(t => t.Comments)
        .ThenInclude(c => c.Author)
    .FirstOrDefaultAsync(t => t.Id == 1);

    var user = await user_repository.GetByIdAsync(4);

    Comment c1, c2;
    if (user is not null && ticket is not null)
    {
        c1 = new(user, "an interesting comment");
        c2 = new(user, "a second interesting comment");
        ticket.AddComment(c1);
        ticket.AddComment(c2);
    }
    await context.SaveChangesAsync();
}