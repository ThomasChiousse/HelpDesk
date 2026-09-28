namespace HelpDesk.Application.Authentication;

public sealed record AuthenticationResult(
    bool IsSuccess,
    string? Token);