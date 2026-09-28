using HelpDesk.Domain;
using System.Security.Claims;

namespace HelpDesk.Api.Authorization
{
    public class HttpCurrentUser : ICurrentUser
    {
        public int Id { get; }
        public UserRole Role { get; }
        private readonly IHttpContextAccessor _contextAccessor;
        public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
        {
            _contextAccessor = httpContextAccessor;
            if (!int.TryParse(_contextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            {
                throw new KeyNotFoundException();
            }

            if (!(Enum.TryParse<UserRole>(_contextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role), out UserRole currentUserRole)
            && Enum.IsDefined<UserRole>(currentUserRole)))
            {
                throw new KeyNotFoundException();
            }
            Id = id;
            Role = currentUserRole;
        }

    }
}
