using HelpDesk.Api.ExceptionHandling;
using HelpDesk.Application.Authentication;
using HelpDesk.Application.Repositories;
using HelpDesk.Application.Services;
using HelpDesk.Infrastructure.Authentication;
using HelpDesk.Infrastructure.Persistence;
using HelpDesk.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using System.Text;

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
builder.Services.AddScoped<IPasswordHasher, AspNetPasswordHasher>();

builder.Services.AddScoped<TicketAssignmentService>();
builder.Services.AddScoped<TicketQueryService>();
builder.Services.AddScoped<TicketCreationService>();
builder.Services.AddScoped<TicketCommentService>();
builder.Services.AddScoped<TicketStatusService>();
builder.Services.AddScoped<TicketUpdateService>();
builder.Services.AddScoped<TicketPatchService>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "JWT signing key is not configured.");

var jwtIssuer = builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "JWT issuer is not configured.");

var jwtAudience = builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "JWT audience is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,

            ValidateAudience = true,
            ValidAudience = jwtAudience,

            ValidateLifetime = true,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();


var app = builder.Build();

app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

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