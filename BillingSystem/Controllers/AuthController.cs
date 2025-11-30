using System.Security.Claims;
using BCrypt.Net;
using BillingSystem.Infrastructure.Data;
using BillingSystem.Core.Models;
using BillingSystem.Core.DTOs;
using BillingSystem.Core.Interfaces;
using BillingSystem.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BillingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IDbContextFactory<ApplicationDbContext> _dbFactory;
    private readonly ITokenService _tokenService;

    public AuthController(IDbContextFactory<ApplicationDbContext> dbFactory, ITokenService tokenService)
    {
        _dbFactory = dbFactory;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await using var _db = await _dbFactory.CreateDbContextAsync();
        
        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if (exists)
            return BadRequest(new { message = "Email already registered." });

        // 🔒 SECURITY: Force all new registrations to Customer role only
        // Admin and Accountant roles can only be assigned by existing Admins
        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = AppRoles.Customer, // ✅ Ignore request.Role - always Customer
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var token = _tokenService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        await using var _db = await _dbFactory.CreateDbContextAsync();
        
        var user = await _db.Users.SingleOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
            return Unauthorized(new { message = "Invalid credentials." });

        var isValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
        if (!isValid)
            return Unauthorized(new { message = "Invalid credentials." });

        var token = _tokenService.GenerateToken(user);

        return Ok(new AuthResponse
        {
            Token = token,
            FullName = user.FullName,
            Email = user.Email,
            Role = user.Role
        });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        var id = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var email = User.FindFirstValue(ClaimTypes.Email);
        var role = User.FindFirstValue(ClaimTypes.Role);

        return Ok(new { id, email, role });
    }
}
