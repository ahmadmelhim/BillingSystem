using System.ComponentModel.DataAnnotations;

namespace BillingSystem.Core.Models
{
    public class Customer
    {
        public int Id { get; set; }

        [Required, MaxLength(150)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(256)]
        public string? Email { get; set; }

        [MaxLength(50)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // 🔴 المهم هنا: نخليها اختيارية
        public int? UserId { get; set; }          // كانت int
        public User? User { get; set; }           // كانت ممكن بدون ?

        public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
