using HelpDesk.Domain;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace HelpDesk.Api.Authorization;

public class CanViewTicketHandler : AuthorizationHandler<CanViewTicketRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        CanViewTicketRequirement requirement)
    {
        var currentUserRole = context.User.FindFirstValue(ClaimTypes.Role);
        if (currentUserRole == "Administrator" || currentUserRole == "Technician")
        {
            context.Succeed(requirement);
        }
        else if (currentUserRole == "User")
        {
            var ticket = context.Resource as Ticket;
            var currentUserId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (ticket?.Requester?.Id.ToString() == currentUserId)
            {
                context.Succeed(requirement);
            }
        }
        return Task.CompletedTask;
    }
}
