using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingSystem.Core.Models;

[Table("Users")]
public class User
{
    public int Id { get; set; }

    [Required, MaxLength(150)]
    public string FullName { get; set; } = default!;

    [Required, MaxLength(256)]
    public string Email { get; set; } = default!;

    [Required]
    public string PasswordHash { get; set; } = default!;

    [Required, MaxLength(50)]
    public string Role { get; set; } = "Customer";  // Admin / Accountant / Customer

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
