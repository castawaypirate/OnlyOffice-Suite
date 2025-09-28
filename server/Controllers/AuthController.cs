using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlyOfficeServer.Data;
using OnlyOfficeServer.Models;
using OnlyOfficeServer.Services;

namespace OnlyOfficeServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly FileService _fileService;
    private const string UserIdSessionKey = "UserId";
    private const string UsernameSessionKey = "Username";

    public AuthController(AppDbContext context, FileService fileService)
    {
        _context = context;
        _fileService = fileService;
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
        // Get current user ID before clearing session
        var userId = HttpContext.Session.GetInt32(UserIdSessionKey);

        // Clean up temp files for this user
        if (userId.HasValue)
        {
            _fileService.CleanupUserTempFiles(userId.Value);
        }

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