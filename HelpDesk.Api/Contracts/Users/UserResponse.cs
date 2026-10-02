namespace HelpDesk.Api.Contracts.Users;

public sealed record UserResponse(
    int Id,
    string Firstname,
    string Lastname,
    string Email,
    string Role);
