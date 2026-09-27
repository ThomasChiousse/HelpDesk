using HelpDesk.Domain;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HelpDesk.Api.Authorization;

public class CanViewTicketHandler : AuthorizationHandler<CanViewTicketRequirement, Ticket>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanViewTicketRequirement requirement,
        Ticket ticket)
    {
        if (context.User.IsInRole(nameof(UserRole.Administrator))
            || context.User.IsInRole(nameof(UserRole.Technician)))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (context.User.IsInRole(nameof(UserRole.User))
            && int.TryParse(
                context.User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var currentUserId)
            && ticket.Requester?.Id == currentUserId)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
