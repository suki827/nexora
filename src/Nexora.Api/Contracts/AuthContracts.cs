using System.ComponentModel.DataAnnotations;

namespace Nexora.Api.Contracts;

public sealed class RegisterRequest
{
    [Required, EmailAddress, StringLength(256)]
    public string Email { get; init; } = string.Empty;
    [Required, MinLength(12), MaxLength(128)]
    public string Password { get; init; } = string.Empty;
    [StringLength(100)]
    public string? DisplayName { get; init; }
}

public sealed class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; init; } = string.Empty;
    [Required]
    public string Password { get; init; } = string.Empty;
}

public sealed record CurrentUserResponse(Guid Id, string Email, string? DisplayName);
