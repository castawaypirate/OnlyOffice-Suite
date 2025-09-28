using Microsoft.AspNetCore.Mvc;
using OnlyOfficeServer.Services;

namespace OnlyOfficeServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FilesController : BaseController
{
    private readonly FileService _fileService;
    private readonly IConfiguration _configuration;

    public FilesController(FileService fileService, IConfiguration configuration)
    {
        _fileService = fileService;
        _configuration = configuration;
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
            var tempFile = await _fileService.SaveTempFileAsync(file, userId);

            return Ok(new
            {
                id = tempFile.Id,
                originalName = tempFile.OriginalName,
                size = _fileService.GetFileSize(tempFile.TempFilePath),
                uploadedAt = tempFile.UploadedAt,
                isTemporary = true,
                message = "File uploaded to temporary storage"
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
            // Get saved files
            var savedFiles = await _fileService.GetUserFilesAsync(userId);
            var savedFileList = savedFiles.Select(f => new
            {
                id = (object)f.Id,
                name = f.OriginalName,
                filename = f.Filename,
                size = _fileService.GetFileSize(f.FilePath),
                uploadDate = f.UploadedAt,
                token = f.Token,
                isTemporary = false
            });

            // Get temp files
            var tempFiles = _fileService.GetUserTempFiles(userId);
            var tempFileList = tempFiles.Select(tf => new
            {
                id = (object)tf.Id,
                name = tf.OriginalName,
                filename = (string?)null,
                size = _fileService.GetFileSize(tf.TempFilePath),
                uploadDate = tf.UploadedAt,
                token = (string?)null,
                isTemporary = true
            });

            // Combine both lists
            var allFiles = savedFileList.Concat(tempFileList).OrderByDescending(f => f.uploadDate);

            var onlyOfficeEnabled = _configuration.GetValue<bool>("OnlyOffice:Enabled", false);

            var response = new
            {
                files = allFiles,
                features = new
                {
                    onlyOfficeEnabled = onlyOfficeEnabled
                }
            };

            return Ok(response);
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

    [HttpPost("{id}/save")]
    public async Task<IActionResult> SaveTempFile(string id)
    {
        var authCheck = RequireAuthentication();
        if (authCheck is not OkResult)
            return authCheck;

        var userId = GetCurrentUserId()!.Value;

        try
        {
            var fileEntity = await _fileService.SaveTempFileToStorageAsync(id, userId);

            return Ok(new
            {
                id = fileEntity.Id,
                originalName = fileEntity.OriginalName,
                filename = fileEntity.Filename,
                size = _fileService.GetFileSize(fileEntity.FilePath),
                uploadedAt = fileEntity.UploadedAt,
                token = fileEntity.Token,
                isTemporary = false,
                message = "File saved successfully"
            });
        }
        catch (FileNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "File save failed", error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFile(string id)
    {
        var authCheck = RequireAuthentication();
        if (authCheck is not OkResult)
            return authCheck;

        var userId = GetCurrentUserId()!.Value;

        try
        {
            // Try to parse as integer for saved files
            if (int.TryParse(id, out var fileId))
            {
                var success = await _fileService.DeleteFileAsync(fileId, userId);
                if (success)
                {
                    return Ok(new { message = "File deleted successfully" });
                }
            }

            // Try as temp file ID
            var tempDeleted = _fileService.DeleteTempFile(id, userId);
            if (tempDeleted)
            {
                return Ok(new { message = "Temp file deleted successfully" });
            }

            return NotFound(new { message = "File not found" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "File deletion failed", error = ex.Message });
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