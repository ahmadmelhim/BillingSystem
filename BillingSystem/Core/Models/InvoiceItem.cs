using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BillingSystem.Core.Models;

[Table("InvoiceItems")]
public class InvoiceItem
{
    public int Id { get; set; }

    [Required]
    public int InvoiceId { get; set; }
    public Invoice? Invoice { get; set; }

    [Required, MaxLength(300)]
    public string Description { get; set; } = default!;

    [Range(0.01, double.MaxValue, ErrorMessage = "الكمية يجب أن تكون أكبر من صفر.")]
    public decimal Quantity { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "سعر الوحدة غير صالح.")]
    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }
}
