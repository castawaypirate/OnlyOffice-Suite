using System.Text;
using Microsoft.AspNetCore.Mvc;
using OnlyOfficeServer.Services;

namespace OnlyOfficeServer.Controllers;

[Route("webdav")]
public class WebDavController : ControllerBase
{
    private readonly WebDavService _webDavService;

    public WebDavController(WebDavService webDavService)
    {
        _webDavService = webDavService;
    }

    [HttpOptions("{userId}")]
    [HttpOptions("{userId}/{*path}")]
    public IActionResult Options()
    {
        Response.Headers.Append("Allow", "OPTIONS, PROPFIND, GET, PUT, DELETE");
        Response.Headers.Append("DAV", "1, 2");
        Response.Headers.Append("MS-Author-Via", "DAV");
        return Ok();
    }

    [HttpGet("{userId}")]
    [HttpGet("{userId}/{*path}")]
    public async Task<IActionResult> PropFind(int userId, string? path = null)
    {
        // WebDAV PROPFIND is sent as a method override or custom method
        if (Request.Method != "PROPFIND" && !Request.Headers.ContainsKey("X-HTTP-Method-Override"))
        {
            return await GetFile(userId, path);
        }

        return await HandlePropFind(userId, path);
    }

    // Handle PROPFIND method
    private async Task<IActionResult> HandlePropFind(int userId, string? path)
    {
        var authResult = await AuthenticateBasic();
        if (authResult.user == null)
        {
            Response.Headers.Append("WWW-Authenticate", "Basic realm=\"WebDAV\"");
            return StatusCode(401, "Authentication required");
        }

        if (authResult.user.Id != userId)
        {
            return StatusCode(403, "Access denied");
        }

        try
        {
            // For now, we only support listing the root directory
            // In a full implementation, you'd handle subdirectories
            if (!string.IsNullOrEmpty(path))
            {
                // Try to get specific file
                var file = await _webDavService.GetFileByNameAsync(userId, Uri.UnescapeDataString(path));
                if (file == null)
                {
                    return NotFound();
                }
            }

            var files = await _webDavService.GetUserFilesAsync(userId);
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var xml = _webDavService.GeneratePropfindResponse(files, baseUrl, userId);

            Response.ContentType = "application/xml; charset=utf-8";
            Response.StatusCode = 207; // Multi-Status
            return Content(xml, "application/xml", Encoding.UTF8);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error processing PROPFIND: {ex.Message}");
        }
    }

    // Handle GET requests for file downloads
    private async Task<IActionResult> GetFile(int userId, string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return BadRequest("File path required");
        }

        var authResult = await AuthenticateBasic();
        if (authResult.user == null)
        {
            Response.Headers.Append("WWW-Authenticate", "Basic realm=\"WebDAV\"");
            return StatusCode(401, "Authentication required");
        }

        // Debug logging
        Console.WriteLine($"WebDAV GET - User ID from DB: {authResult.user.Id}, Username: {authResult.user.Username}");
        Console.WriteLine($"WebDAV GET - Requested userId from URL: {userId}");

        if (authResult.user.Id != userId)
        {
            return StatusCode(403, $"Access denied. User {authResult.user.Username} (ID: {authResult.user.Id}) cannot access userId {userId}");
        }

        try
        {
            var fileName = Uri.UnescapeDataString(path);
            var file = await _webDavService.GetFileByNameAsync(userId, fileName);
            
            if (file == null || !System.IO.File.Exists(file.FilePath))
            {
                return NotFound();
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(file.FilePath);
            var contentType = GetContentType(file.OriginalName);

            return File(fileBytes, contentType, file.OriginalName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving file: {ex.Message}");
        }
    }

    [HttpPut("{userId}/{*path}")]
    public async Task<IActionResult> PutFile(int userId, string path)
    {
        var authResult = await AuthenticateBasic();
        if (authResult.user == null)
        {
            Response.Headers.Append("WWW-Authenticate", "Basic realm=\"WebDAV\"");
            return StatusCode(401, "Authentication required");
        }

        if (authResult.user.Id != userId)
        {
            return StatusCode(403, "Access denied");
        }

        try
        {
            var fileName = Uri.UnescapeDataString(path);
            
            // Check if file already exists
            var existingFile = await _webDavService.GetFileByNameAsync(userId, fileName);
            var isUpdate = existingFile != null;

            await _webDavService.SaveFileAsync(userId, fileName, Request.Body);

            return StatusCode(isUpdate ? 204 : 201); // No Content or Created
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error uploading file: {ex.Message}");
        }
    }

    [HttpDelete("{userId}/{*path}")]
    public async Task<IActionResult> DeleteFile(int userId, string path)
    {
        var authResult = await AuthenticateBasic();
        if (authResult.user == null)
        {
            Response.Headers.Append("WWW-Authenticate", "Basic realm=\"WebDAV\"");
            return StatusCode(401, "Authentication required");
        }

        if (authResult.user.Id != userId)
        {
            return StatusCode(403, "Access denied");
        }

        try
        {
            var fileName = Uri.UnescapeDataString(path);
            var success = await _webDavService.DeleteFileAsync(userId, fileName);
            
            if (!success)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error deleting file: {ex.Message}");
        }
    }

    // Custom method handler for PROPFIND
    [HttpPost("{userId}")]
    [HttpPost("{userId}/{*path}")]
    public async Task<IActionResult> HandleCustomMethod(int userId, string? path = null)
    {
        var method = Request.Headers["X-HTTP-Method-Override"].FirstOrDefault() ?? Request.Method;
        
        return method.ToUpper() switch
        {
            "PROPFIND" => await HandlePropFind(userId, path),
            _ => BadRequest($"Unsupported method: {method}")
        };
    }

    private async Task<(Models.User? user, string? error)> AuthenticateBasic()
    {
        var authHeader = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Basic "))
        {
            return (null, "Missing Authorization header");
        }

        try
        {
            var encodedCredentials = authHeader.Substring("Basic ".Length);
            var credentials = Encoding.UTF8.GetString(Convert.FromBase64String(encodedCredentials));
            var parts = credentials.Split(':', 2);
            
            if (parts.Length != 2)
            {
                return (null, "Invalid credentials format");
            }

            var username = parts[0];
            var password = parts[1];

            var user = await _webDavService.AuthenticateUserAsync(username, password);
            return (user, user == null ? "Invalid credentials" : null);
        }
        catch (Exception ex)
        {
            return (null, $"Authentication error: {ex.Message}");
        }
    }

    private string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".pdf" => "application/pdf",
            ".doc" => "application/msword",
            ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xls" => "application/vnd.ms-excel",
            ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            ".ppt" => "application/vnd.ms-powerpoint",
            ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            ".txt" => "text/plain",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            _ => "application/octet-stream"
        };
    }
}