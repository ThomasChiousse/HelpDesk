using HelpDesk.Api.Authorization;
using HelpDesk.Api.ExceptionHandling;
using HelpDesk.Application.Authentication;
using HelpDesk.Application.Repositories;
using HelpDesk.Application.Services.Tickets;
using HelpDesk.Application.Services.Users;
using HelpDesk.Infrastructure.Authentication;
using HelpDesk.Infrastructure.Persistence;
using HelpDesk.Infrastructure.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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

builder.Services
    .AddScoped<ITicketRepository, TicketRepository>()
    .AddScoped<IUserRepository, UserRepository>()
    .AddScoped<IPasswordHasher, AspNetPasswordHasher>()
    .AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

builder.Services
    .AddScoped<TicketAssignmentService>()
    .AddScoped<TicketQueryService>()
    .AddScoped<TicketCreationService>()
    .AddScoped<TicketCommentService>()
    .AddScoped<TicketStatusService>()
    .AddScoped<TicketUpdateService>()
    .AddScoped<TicketPatchService>()
    .AddScoped<AuthenticationService>()
    .AddScoped<UserRegistrationService>();

builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddHttpContextAccessor();

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

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("CanManageTickets", policy =>
        policy.RequireRole(["Technician", "Administrator"]))
    .AddPolicy("CanViewTicket", policy =>
        policy.AddRequirements(new CanViewTicketRequirement()));

builder.Services.AddSingleton<IAuthorizationHandler, CanViewTicketHandler>();

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