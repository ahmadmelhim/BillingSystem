using System.ComponentModel.DataAnnotations;

namespace BillingSystem.Core.Models;

public class Invoice
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public DateTime DateIssued { get; set; }
    public DateTime? DueDate { get; set; }

    public string Status { get; set; } = "Pending";

    public decimal TotalAmount { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<InvoiceItem> Items { get; set; } = new();

    // 🔹 علاقـة الدفعات
    public List<Payment> Payments { get; set; } = new();

    // 🔹 User ownership for data isolation
    public int UserId { get; set; }
    public User? User { get; set; }

    // 🔹 مجموع المدفوع (يُحسب من الدفعات)
    public decimal PaidAmount => Payments?.Sum(p => p.Amount) ?? 0m;

    // 🔹 المتبقي
    public decimal RemainingAmount => TotalAmount - PaidAmount;
}
