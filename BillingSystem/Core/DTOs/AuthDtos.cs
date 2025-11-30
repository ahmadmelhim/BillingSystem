using System.ComponentModel.DataAnnotations;

namespace BillingSystem.Core.DTOs;

public class RegisterRequest
{
    [Required, MaxLength(150)]
    public string FullName { get; set; } = default!;

    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required, MinLength(6)]
    public string Password { get; set; } = default!;

    [Required]
    public string Role { get; set; } = "Customer"; // Admin / Accountant / Customer
}

public class LoginRequest
{
    [Required, EmailAddress]
    public string Email { get; set; } = default!;

    [Required]
    public string Password { get; set; } = default!;
}

public class AuthResponse
{
    public string Token { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Role { get; set; } = default!;
}
