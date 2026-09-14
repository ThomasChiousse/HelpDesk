using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HelpDesk.Infrastructure.Persistence;

public class HelpDeskDbContextFactory
    : IDesignTimeDbContextFactory<HelpDeskDbContext>
{
    public HelpDeskDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder =
            new DbContextOptionsBuilder<HelpDeskDbContext>();

        optionsBuilder.UseSqlServer(
            @"Server=(localdb)\MSSQLLocalDB;
              Database=HelpDeskDb;
              Trusted_Connection=True;
              TrustServerCertificate=True;");

        return new HelpDeskDbContext(optionsBuilder.Options);
    }
}