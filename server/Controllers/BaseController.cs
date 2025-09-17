using Microsoft.AspNetCore.Mvc;

namespace OnlyOfficeServer.Controllers;

public class BaseController : ControllerBase
{
    private const string UserIdSessionKey = "UserId";
    private const string UsernameSessionKey = "Username";

    protected int? GetCurrentUserId()
    {
        return HttpContext.Session.GetInt32(UserIdSessionKey);
    }

    protected string? GetCurrentUsername()
    {
        return HttpContext.Session.GetString(UsernameSessionKey);
    }

    protected bool IsAuthenticated()
    {
        return GetCurrentUserId().HasValue;
    }

    protected IActionResult RequireAuthentication()
    {
        if (!IsAuthenticated())
        {
            return Unauthorized(new { message = "Authentication required" });
        }
        return Ok();
    }
}