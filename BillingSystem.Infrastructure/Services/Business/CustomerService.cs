using BillingSystem.Core.Interfaces;
using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Core.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BillingSystem.Infrastructure.Services.Business;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IInvoiceRepository _invoiceRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CustomerService> _logger;

    public CustomerService(
        ICustomerRepository customerRepository,
        IInvoiceRepository invoiceRepository,
        ICurrentUserService currentUserService,
        ILogger<CustomerService> logger)
    {
        _customerRepository = customerRepository;
        _invoiceRepository = invoiceRepository;
        _currentUserService = currentUserService;
        _logger = logger;
    }


    public async Task<(IReadOnlyList<Customer> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, int pageSize, string? search)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
            return (Array.Empty<Customer>(), 0);

        return await _customerRepository.GetPagedAsync(currentUserId.Value, pageIndex, pageSize, search);
    }


    public async Task<Customer?> GetByIdAsync(int id)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
            return null;

        return await _customerRepository.GetByIdAsync(id);
    }


    public async Task<Customer> CreateAsync(Customer customer)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Attempt to create customer without authentication");
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        try
        {
            customer.CreatedAt = DateTime.UtcNow;
            customer.UserId = currentUserId.Value;

            await _customerRepository.AddAsync(customer);

            _logger.LogInformation("Created customer {CustomerName} (ID: {CustomerId}) for user {UserId}", 
                customer.Name, customer.Id, currentUserId.Value);

            return customer;
        }
        catch (DbUpdateException ex) when (IsEmailDuplicateException(ex))
        {
            _logger.LogWarning("Duplicate email {Email} for customer creation", customer.Email);
            throw new InvalidOperationException("البريد الإلكتروني مسجل بالفعل لعميل آخر.");
        }
    }

    public async Task UpdateAsync(Customer customer)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Attempt to update customer without authentication");
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        var existing = await _customerRepository.GetByIdAsync(customer.Id);

        if (existing == null || existing.UserId != currentUserId.Value)
        {
            _logger.LogWarning("Customer {CustomerId} not found for user {UserId}", customer.Id, currentUserId.Value);
            throw new KeyNotFoundException("العميل غير موجود.");
        }

        existing.Name = customer.Name;
        existing.Email = customer.Email;
        existing.Phone = customer.Phone;
        existing.Address = customer.Address;

        try
        {
            await _customerRepository.UpdateAsync(existing);
            _logger.LogInformation("Updated customer {CustomerName} (ID: {CustomerId})", customer.Name, customer.Id);
        }
        catch (DbUpdateException ex) when (IsEmailDuplicateException(ex))
        {
            _logger.LogWarning("Duplicate email {Email} for customer update", customer.Email);
            throw new InvalidOperationException("البريد الإلكتروني مسجل بالفعل لعميل آخر.");
        }
    }

    public async Task DeleteAsync(int id)
    {
        var currentUserId = await _currentUserService.GetCurrentUserIdAsync();
        if (!currentUserId.HasValue)
        {
            _logger.LogWarning("Attempt to delete customer without authentication");
            throw new UnauthorizedAccessException("User not authenticated.");
        }

        // Check if customer has invoices using repository
        var invoices = await _invoiceRepository.GetAllAsync();
        var hasInvoices = invoices.Any(i => i.CustomerId == id);

        if (hasInvoices)
        {
            _logger.LogWarning("Attempt to delete customer {CustomerId} with existing invoices", id);
            throw new InvalidOperationException("لا يمكن حذف العميل لوجود فواتير مرتبطة به.");
        }

        var existing = await _customerRepository.GetByIdAsync(id);

        if (existing == null || existing.UserId != currentUserId.Value)
        {
            _logger.LogWarning("Customer {CustomerId} not found for deletion", id);
            return;
        }

        await _customerRepository.DeleteAsync(id);

        _logger.LogInformation("Deleted customer {CustomerName} (ID: {CustomerId})", existing.Name, id);
    }

    // ===== Helper ??????? ??? ??????? ?????? =====
    private static bool IsEmailDuplicateException(DbUpdateException ex)
    {
        if (ex.InnerException is SqlException sqlEx)
        {
            // 2601 ?? 2627 = duplicate key
            if ((sqlEx.Number == 2601 || sqlEx.Number == 2627) &&
                sqlEx.Message.Contains("IX_Customers_Email"))
            {
                return true;
            }
        }

        return false;
    }
}
