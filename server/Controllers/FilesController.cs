using Microsoft.AspNetCore.Mvc;
using OnlyOfficeServer.Services;

namespace OnlyOfficeServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : BaseController
{
    private readonly FileService _fileService;

    public FilesController(FileService fileService)
    {
        _fileService = fileService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        var authCheck = RequireAuthentication();
        if (authCheck is not OkResult)
            return authCheck;

        var userId = GetCurrentUserId()!.Value;

        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "No file provided" });
        }

        // Check file size (limit to 100MB for now)
        if (file.Length > 100 * 1024 * 1024)
        {
            return BadRequest(new { message = "File too large. Maximum size is 100MB." });
        }

        try
        {
            var fileEntity = await _fileService.SaveFileAsync(file, userId);
            
            return Ok(new
            {
                id = fileEntity.Id,
                originalName = fileEntity.OriginalName,
                filename = fileEntity.Filename,
                size = _fileService.GetFileSize(fileEntity.FilePath),
                uploadedAt = fileEntity.UploadedAt,
                message = "File uploaded successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "File upload failed", error = ex.Message });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetFiles()
    {
        var authCheck = RequireAuthentication();
        if (authCheck is not OkResult)
            return authCheck;

        var userId = GetCurrentUserId()!.Value;

        try
        {
            var files = await _fileService.GetUserFilesAsync(userId);
            
            var fileList = files.Select(f => new
            {
                id = f.Id,
                name = f.OriginalName,
                filename = f.Filename,
                size = _fileService.GetFileSize(f.FilePath),
                uploadDate = f.UploadedAt,
                token = f.Token
            });

            return Ok(fileList);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to retrieve files", error = ex.Message });
        }
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadFile(int id)
    {
        var authCheck = RequireAuthentication();
        if (authCheck is not OkResult)
            return authCheck;

        var userId = GetCurrentUserId()!.Value;

        try
        {
            var fileEntity = await _fileService.GetFileByIdAsync(id, userId);
            
            if (fileEntity == null)
            {
                return NotFound(new { message = "File not found" });
            }

            if (!System.IO.File.Exists(fileEntity.FilePath))
            {
                return NotFound(new { message = "File not found on disk" });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fileEntity.FilePath);
            var contentType = GetContentType(fileEntity.OriginalName);

            return File(fileBytes, contentType, fileEntity.OriginalName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "File download failed", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(int id)
    {
        var authCheck = RequireAuthentication();
        if (authCheck is not OkResult)
            return authCheck;

        var userId = GetCurrentUserId()!.Value;

        try
        {
            var success = await _fileService.DeleteFileAsync(id, userId);
            
            if (!success)
            {
                return NotFound(new { message = "File not found" });
            }

            return Ok(new { message = "File deleted successfully" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "File deletion failed", error = ex.Message });
        }
    }

    [HttpGet("onlyoffice/download/{id}")]
    public async Task<IActionResult> OnlyOfficeDownload(int id)
    {
        try
        {
            var fileEntity = await _fileService.GetFileByIdAsync(id);
            
            if (fileEntity == null)
            {
                return NotFound(new { message = "File not found" });
            }

            if (!System.IO.File.Exists(fileEntity.FilePath))
            {
                return NotFound(new { message = "File not found on disk" });
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fileEntity.FilePath);
            var contentType = GetContentType(fileEntity.OriginalName);

            return File(fileBytes, contentType, fileEntity.OriginalName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "File download failed", error = ex.Message });
        }
    }

    [HttpGet("onlyoffice/config/{id}")]
    public async Task<IActionResult> OnlyOfficeConfig(int id)
    {
        try
        {
            Console.WriteLine($"🔧 OnlyOffice config requested for file ID: {id}");
            
            var fileEntity = await _fileService.GetFileByIdAsync(id);
            
            if (fileEntity == null)
            {
                Console.WriteLine($"❌ File not found with ID: {id}");
                return NotFound(new { message = "File not found" });
            }

            Console.WriteLine($"🔧 File found: {fileEntity.OriginalName}, Path: {fileEntity.FilePath}");

            if (!System.IO.File.Exists(fileEntity.FilePath))
            {
                Console.WriteLine($"❌ File not found on disk: {fileEntity.FilePath}");
                return NotFound(new { message = "File not found on disk" });
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            Console.WriteLine($"🔧 Base URL: {baseUrl}");
            
            var config = new
            {
                document = new
                {
                    fileType = GetFileExtension(fileEntity.OriginalName),
                    key = $"file-{fileEntity.Id}-{DateTime.UtcNow:yyyyMMddHHmmssffff}",
                    title = fileEntity.OriginalName,
                    url = $"{baseUrl}/api/files/onlyoffice/download/{fileEntity.Id}",
                    permissions = new
                    {
                        edit = true,
                        download = true,
                        print = true
                    }
                },
                documentType = GetDocumentType(fileEntity.OriginalName),
                editorConfig = new
                {
                    mode = "edit"
                }
            };

            Console.WriteLine($"🔧 Generated config: {System.Text.Json.JsonSerializer.Serialize(config, new System.Text.Json.JsonSerializerOptions { WriteIndented = true })}");

            return Ok(config);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Config generation failed: {ex.Message}");
            return StatusCode(500, new { message = "Config generation failed", error = ex.Message });
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

    private string GetFileExtension(string fileName)
    {
        return Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
    }

    private string GetDocumentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".doc" or ".docx" => "word",
            ".xls" or ".xlsx" => "cell",
            ".ppt" or ".pptx" => "slide",
            _ => "word"
        };
    }
}