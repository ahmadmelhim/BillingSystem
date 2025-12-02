using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Core.Models;

namespace BillingSystem.Infrastructure.Services.Business;

public interface IUserService
{
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(int pageIndex, int pageSize, string? search = null, string? role = null);
    Task<User?> GetByIdAsync(int id);
    Task<User> CreateAsync(User user, string password);
    Task UpdateAsync(User user);
    Task DeleteAsync(int id);
}

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;

    public UserService(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int pageIndex, 
        int pageSize, 
        string? search = null,
        string? role = null)
    {
        return await _userRepository.GetPagedAsync(pageIndex, pageSize, search, role);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _userRepository.GetByIdAsync(id);
    }

    public async Task<User> CreateAsync(User user, string password)
    {
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
        return await _userRepository.CreateUserAsync(user, passwordHash);
    }

    public async Task UpdateAsync(User user)
    {
        await _userRepository.UpdateUserAsync(user);
    }

    public async Task DeleteAsync(int id)
    {
        var user = await _userRepository.GetByIdAsync(id);
        if (user != null)
        {
            await _userRepository.DeleteAsync(id);
        }
    }
}

