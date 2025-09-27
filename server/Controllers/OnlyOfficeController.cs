using Microsoft.AspNetCore.Mvc;
using OnlyOfficeServer.Managers;
using OnlyOfficeServer.Repositories;

namespace OnlyOfficeServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OnlyOfficeController : ControllerBase
{
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
                
                var manager = new OnlyOfficeManager(repository, configuration!);
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
        // Manual using statement for resource management
        using (var repository = new OnlyOfficeRepository())
        {
            try
            {
                // Get configuration from appsettings
                var configuration = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                
                var manager = new OnlyOfficeManager(repository, configuration!);
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
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
                            mode = config.EditorConfig.Mode
                        },
                        token = config.Token,
                    },
                    onlyOfficeServerUrl = config.OnlyOfficeServerUrl
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

}