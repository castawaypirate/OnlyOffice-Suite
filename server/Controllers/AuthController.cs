using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlyOfficeServer.Data;
using OnlyOfficeServer.Models;

namespace OnlyOfficeServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private const string UserIdSessionKey = "UserId";
    private const string UsernameSessionKey = "Username";

    public AuthController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Username and password are required" });
        }

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.Password == request.Password);

        if (user == null)
        {
            return Unauthorized(new { message = "Invalid credentials" });
        }

        // Create session
        HttpContext.Session.SetInt32(UserIdSessionKey, user.Id);
        HttpContext.Session.SetString(UsernameSessionKey, user.Username);

        return Ok(new { 
            message = "Login successful", 
            userId = user.Id,
            username = user.Username 
        });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return Ok(new { message = "Logout successful" });
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        var userId = HttpContext.Session.GetInt32(UserIdSessionKey);
        var username = HttpContext.Session.GetString(UsernameSessionKey);

        if (userId.HasValue && !string.IsNullOrEmpty(username))
        {
            return Ok(new { 
                isAuthenticated = true, 
                userId = userId.Value,
                username = username 
            });
        }

        return Ok(new { isAuthenticated = false });
    }
}

public class LoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}