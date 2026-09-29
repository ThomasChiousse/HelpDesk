using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Users;

public sealed class RegisterUserRequest
{
    [Required]
    [MaxLength(100)]
    public string Firstname { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Lastname { get; init; } = string.Empty;

    [Required]
    [MaxLength(255)]
    public string Email { get; init; } = string.Empty;

    [Required]
    [MinLength(8)]
    public string Password { get; init; } = string.Empty;
}
