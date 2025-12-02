using BillingSystem.Core.Models;

namespace BillingSystem.Core.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
}
