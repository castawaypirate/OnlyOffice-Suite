using Microsoft.AspNetCore.Mvc;
using OnlyOfficeServer.Managers;
using OnlyOfficeServer.Repositories;
using OnlyOfficeServer.Data;
using OnlyOfficeServer.Models;

namespace OnlyOfficeServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OnlyOfficeController : BaseController
{
    private readonly InstallationManager _installationManager;

    public OnlyOfficeController(InstallationManager installationManager)
    {
        _installationManager = installationManager;
    }
    [HttpGet("download/{id}")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        // Manual using statement for resource management
        using (var repository = new OnlyOfficeRepository())
        {
            try
            {
                // Get configuration from appsettings
                var configuration = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                var context = HttpContext.RequestServices.GetService(typeof(AppDbContext)) as AppDbContext;

                // Create InstallationRepository for consistency with config endpoint
                using var installationRepository = new InstallationRepository();
                var manager = new OnlyOfficeManager(repository, configuration!, context!, installationRepository);
                var fileResult = await manager.GetFileForDownloadAsync(id);
                
                return File(fileResult.Content, fileResult.ContentType, fileResult.FileName);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "File download failed", error = ex.Message });
            }
        }
    }

    [HttpGet("config/{id}")]
    public async Task<IActionResult> GetConfig(int id)
    {
        var authCheck = RequireAuthentication();
        if (authCheck is not OkResult)
            return authCheck;

        var userId = GetCurrentUserId()!.Value;

        // Manual using statement for resource management
        using (var repository = new OnlyOfficeRepository())
        {
            try
            {
                // Get configuration from appsettings
                var configuration = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                var context = HttpContext.RequestServices.GetService(typeof(AppDbContext)) as AppDbContext;

                // Create InstallationRepository for Approach 3
                using var installationRepository = new InstallationRepository();
                var manager = new OnlyOfficeManager(repository, configuration!, context!, installationRepository);

                // Get ApplicationId from configuration
                var applicationId = configuration.GetValue<int>("ApplicationId");

                // **APPROACH 1: Using dedicated InstallationManager (injected via DI)**
                var baseUrl = await _installationManager.GetApplicationUrlAsync(applicationId);

                // **APPROACH 2: Using OnlyOfficeManager installation methods (via AppDbContext)**
                // var baseUrl = await manager.GetApplicationUrlAsync(applicationId);

                // **APPROACH 3: Using OnlyOfficeManager installation methods (via InstallationRepository)**
                // var baseUrl = await manager.GetApplicationUrlViaRepositoryAsync(applicationId);

                var config = await manager.GetConfigAsync(id, baseUrl);

                // Convert business result to API response format (now includes JWT token)
                var response = new
                {
                    config = new
                    {
                        document = new
                        {
                            fileType = config.Document.FileType,
                            key = config.Document.Key,
                            title = config.Document.Title,
                            url = config.Document.Url,
                            permissions = new
                            {
                                edit = config.Document.Permissions.Edit,
                                download = config.Document.Permissions.Download,
                                print = config.Document.Permissions.Print
                            }
                        },
                        documentType = config.DocumentType,
                        editorConfig = new
                        {
                            mode = config.EditorConfig.Mode,
                            callbackUrl = config.EditorConfig.CallbackUrl
                        },
                        token = config.Token,
                    },
                    onlyOfficeServerUrl = config.OnlyOfficeServerUrl,
                    userId = userId
                };

                return Ok(response);
            }
            catch (FileNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return StatusCode(500, new { message = "Configuration error", error = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Config generation failed", error = ex.Message });
            }
        }
    }

    [HttpGet("test-connectivity")]
    public IActionResult TestConnectivity()
    {
        return Ok(new { message = "OnlyOffice controller is reachable", timestamp = DateTime.UtcNow });
    }

    [HttpPost("callback/{id}")]
    public async Task<IActionResult> HandleCallback(int id, [FromBody] CallbackRequest request)
    {
        var logger = HttpContext.RequestServices.GetService<ILogger<OnlyOfficeController>>();
        var requestId = Guid.NewGuid().ToString("N")[..8];

        logger?.LogInformation("=== CALLBACK START === Request ID: {RequestId}, File ID: {FileId}, Timestamp: {Timestamp}",
            requestId, id, DateTime.UtcNow);

        try
        {
            // Log all incoming request data
            logger?.LogInformation("Callback Request Details - ID: {RequestId}, Status: {Status}, Key: {Key}, URL: {Url}, Users: {Users}",
                requestId, request.Status, request.Key, request.Url ?? "null",
                request.Users != null ? string.Join(",", request.Users) : "null");

            // Log headers for debugging
            logger?.LogInformation("Request Headers - ID: {RequestId}, Content-Type: {ContentType}, User-Agent: {UserAgent}",
                requestId, Request.ContentType, Request.Headers.UserAgent.ToString());

            // Get configuration from appsettings
            var configuration = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
            var context = HttpContext.RequestServices.GetService(typeof(AppDbContext)) as AppDbContext;

            // Manual using statement for resource management
            using (var repository = new OnlyOfficeRepository())
            {
                using var installationRepository = new InstallationRepository();
                var manager = new OnlyOfficeManager(repository, configuration!, context!, installationRepository);

                logger?.LogInformation("Processing callback - ID: {RequestId}, About to call ProcessCallbackAsync", requestId);

                var result = await manager.ProcessCallbackAsync(id, request);

                logger?.LogInformation("Callback processing result - ID: {RequestId}, Success: {Result}", requestId, result);

                var response = new CallbackResponse { Error = result ? 0 : 1, Message = result ? null : "Callback processing failed" };

                logger?.LogInformation("=== CALLBACK END === Request ID: {RequestId}, Response: {Response}",
                    requestId, System.Text.Json.JsonSerializer.Serialize(response));

                return Ok(response);
            }
        }
        catch (FileNotFoundException ex)
        {
            logger?.LogError("FileNotFoundException in callback - ID: {RequestId}, Error: {Error}", requestId, ex.Message);
            var response = new CallbackResponse { Error = 1, Message = ex.Message };
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Exception in callback - ID: {RequestId}, Error: {Error}", requestId, ex.Message);
            // Always return 200 OK to OnlyOffice, but with error code
            var response = new CallbackResponse { Error = 1, Message = $"Callback failed: {ex.Message}" };
            return Ok(response);
        }
    }

}