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
        // .NET Framework 4.5.6 style: Manual using statement for resource management
        using (var repository = new OnlyOfficeRepository())
        {
            try
            {
                // Get configuration from appsettings (in .NET Framework, this would be from DI container or config manager)
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
        // .NET Framework 4.5.6 style: Manual using statement for resource management
        using (var repository = new OnlyOfficeRepository())
        {
            try
            {
                // Get configuration from appsettings (in .NET Framework, this would be from DI container or config manager)
                var configuration = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                
                var manager = new OnlyOfficeManager(repository, configuration!);
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var config = await manager.GetConfigAsync(id, baseUrl);
                
                // Convert business result to API response format (now includes JWT token)
                var response = new
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
                    token = config.Token // JWT token generated in backend
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

    // Option 2: Hardcoded JSON Strings - Force exact TypeScript match
    [HttpGet("config-hardcoded/{id}")]
    public async Task<IActionResult> GetConfigHardcoded(int id)
    {
        using (var repository = new OnlyOfficeRepository())
        {
            try
            {
                var configuration = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                var manager = new OnlyOfficeManager(repository, configuration!);
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var config = await manager.GetConfigWithHardcodedJsonAsync(id, baseUrl);
                
                var response = new
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
                    method = "hardcoded-json" // Debug flag
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Hardcoded config generation failed", error = ex.Message });
            }
        }
    }

    // Option 5: TypeScript-style implementation - Mimic exact JS behavior
    [HttpGet("config-typescript/{id}")]
    public async Task<IActionResult> GetConfigTypeScriptStyle(int id)
    {
        using (var repository = new OnlyOfficeRepository())
        {
            try
            {
                var configuration = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                var manager = new OnlyOfficeManager(repository, configuration!);
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var config = await manager.GetConfigTypeScriptStyleAsync(id, baseUrl);
                
                var response = new
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
                    method = "typescript-style" // Debug flag
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "TypeScript-style config generation failed", error = ex.Message });
            }
        }
    }

    // Alternative: Manual JWT without any JSON library
    [HttpGet("config-manual/{id}")]
    public async Task<IActionResult> GetConfigManual(int id)
    {
        using (var repository = new OnlyOfficeRepository())
        {
            try
            {
                var configuration = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                var manager = new OnlyOfficeManager(repository, configuration!);
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var config = await manager.GetConfigManualJwtAsync(id, baseUrl);
                
                var response = new
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
                    method = "manual-jwt" // Debug flag
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Manual JWT generation failed", error = ex.Message });
            }
        }
    }



    // Method 3: DataContractJsonSerializer
    [HttpGet("config-datacontract/{id}")]
    public async Task<IActionResult> GetConfigDataContract(int id)
    {
        using (var repository = new OnlyOfficeRepository())
        {
            try
            {
                var configuration = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
                var manager = new OnlyOfficeManager(repository, configuration!);
                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var config = await manager.GetConfigDataContractAsync(id, baseUrl);
                
                var response = new
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
                    method = "datacontract-serializer" // Debug flag
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "DataContractJsonSerializer generation failed", error = ex.Message });
            }
        }
    }
}