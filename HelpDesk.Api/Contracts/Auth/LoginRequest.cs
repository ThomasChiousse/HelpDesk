using System.ComponentModel.DataAnnotations;

namespace HelpDesk.Api.Contracts.Auth;

public sealed record LoginRequest(
    [Required]
    [EmailAddress]
    [MaxLength(255)]
    string Email,

    [Required]
    string Password);