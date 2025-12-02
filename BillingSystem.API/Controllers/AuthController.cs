using System.Security.Claims;
using BCrypt.Net;
using BillingSystem.Core.Models;
using BillingSystem.Core.DTOs;
using BillingSystem.Core.Interfaces;
using BillingSystem.Core.Interfaces.Repositories;
using BillingSystem.Core.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BillingSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public AuthController(IUserRepository userRepository, ITokenService tokenService)
    {
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var exists = await _userRepository.EmailExistsAsync(request.Email);
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

        await _userRepository.AddAsync(user);

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

        var user = await _userRepository.GetByEmailAsync(request.Email);
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
