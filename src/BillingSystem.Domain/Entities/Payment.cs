using System.ComponentModel.DataAnnotations;

namespace BillingSystem.Core.Models;

public class Payment
{
    public int Id { get; set; }

    [Required]
    public int InvoiceId { get; set; }

    public Invoice Invoice { get; set; } = null!;

    [Required]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required]
    [Range(0.01, double.MaxValue, ErrorMessage = "قيمة الدفعة يجب أن تكون أكبر من صفر.")]
    public decimal Amount { get; set; }

    [Required]
    [StringLength(50)]
    public string Method { get; set; } = "Cash";

    [StringLength(100)]
    public string? Reference { get; set; }

    [StringLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
