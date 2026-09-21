namespace HelpDesk.Api.Contracts.Auth;

public sealed record CurrentUserResponse(
    string UserId,
    string Email,
    string Role);