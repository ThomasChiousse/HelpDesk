using HelpDesk.Application.Authentication;
using HelpDesk.Domain;
using System.Security.Claims;

namespace HelpDesk.Api.Authorization
{
    public class HttpCurrentUser : ICurrentUser
    {
        public int Id { get; }
        public UserRole Role { get; }

        public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            var user = httpContextAccessor.HttpContext?.User
            ?? throw new InvalidOperationException(
                "No current HTTP user is available.");

            if (!int.TryParse(
                user.FindFirstValue(ClaimTypes.NameIdentifier),
                out var id))
            {
                throw new InvalidOperationException(
                    "Authenticated user does not contain a valid id.");
            }

            if (!Enum.TryParse<UserRole>(
                    user.FindFirstValue(ClaimTypes.Role),
                    out var role)
                || !Enum.IsDefined(role))
            {
                throw new InvalidOperationException(
                    "Authenticated user does not contain a valid role.");
            }

            Id = id;
            Role = role;
        }
    }
}
