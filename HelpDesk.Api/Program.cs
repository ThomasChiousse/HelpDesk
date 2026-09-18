using HelpDesk.Api.ExceptionHandling;
using HelpDesk.Application.Repositories;
using HelpDesk.Application.Services;
using HelpDesk.Infrastructure.Persistence;
using HelpDesk.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();


if (!builder.Environment.IsEnvironment("Testing"))
{
    builder.Services.AddDbContext<HelpDeskDbContext>(
        options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString(
                    "HelpDeskDb")));
}

builder.Services.AddOpenApi();
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

builder.Services.AddScoped<TicketAssignmentService>();
builder.Services.AddScoped<TicketQueryService>();
builder.Services.AddScoped<TicketCreationService>();
builder.Services.AddScoped<TicketCommentService>();
builder.Services.AddScoped<TicketStatusService>();
builder.Services.AddScoped<TicketUpdateService>();
builder.Services.AddScoped<TicketPatchService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapControllers();

app.Run();

public partial class Program()
{

}