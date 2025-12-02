using System.Threading.Tasks;

namespace BillingSystem.Core.Interfaces
{
    public interface IPdfService
    {
        Task<byte[]> GenerateInvoicePdfAsync(int invoiceId);
    }
}
