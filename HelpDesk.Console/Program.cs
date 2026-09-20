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
ticket2?.SetTitle("THIS SHOULD BE SAVED");
await ticket_repository2.SaveChangesAsync();
Console.WriteLine(ticket2?.Title);

