using BillingSystem.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Invoice> Invoices => Set<Invoice>();
    public DbSet<InvoiceItem> InvoiceItems => Set<InvoiceItem>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== Users =====
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // ===== Customers =====
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.Email)
            .IsUnique()
            .HasFilter("[Email] IS NOT NULL");

        // Customer -> Invoices
        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.Customer)
            .WithMany()
            .HasForeignKey(i => i.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // User -> Invoices (for data isolation)
        modelBuilder.Entity<Invoice>()
            .HasOne(i => i.User)
            .WithMany()
            .HasForeignKey(i => i.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ✅ Indexes لتحسين الأداء
        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.UserId);

        modelBuilder.Entity<Invoice>()
            .HasIndex(i => i.Status);

        modelBuilder.Entity<Invoice>()
            .HasIndex(i => new { i.DateIssued, i.DueDate });

        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.UserId);

        // Invoice -> InvoiceItems
        modelBuilder.Entity<InvoiceItem>()
            .HasOne(ii => ii.Invoice)
            .WithMany(i => i.Items)
            .HasForeignKey(ii => ii.InvoiceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Invoice -> Payments
        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<Payment>()
            .HasOne(p => p.Invoice)
            .WithMany(i => i.Payments)
            .HasForeignKey(p => p.InvoiceId)
            .OnDelete(DeleteBehavior.Restrict);

        // ✅ Index على InvoiceId للمدفوعات
        modelBuilder.Entity<Payment>()
            .HasIndex(p => p.InvoiceId);

        // ??? ??????? ?? Invoice / InvoiceItem
        modelBuilder.Entity<Invoice>()
            .Property(i => i.TotalAmount)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<InvoiceItem>()
            .Property(ii => ii.Quantity)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<InvoiceItem>()
            .Property(ii => ii.UnitPrice)
            .HasColumnType("decimal(18,2)");

        modelBuilder.Entity<InvoiceItem>()
            .Property(ii => ii.TotalPrice)
            .HasColumnType("decimal(18,2)");
    }
}

