using Microsoft.AspNetCore.Mvc;
using OnlyOfficeServer.Services;

namespace OnlyOfficeServer.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OnlyOfficeController : BaseController
{
    private readonly FileService _fileService;

    public OnlyOfficeController(FileService fileService)
    {
        _fileService = fileService;
    }

    [HttpGet("download/{id}")]
    public async Task<IActionResult> DownloadFile(int id)
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

    [HttpGet("config/{id}")]
    public async Task<IActionResult> GetConfig(int id)
    {
        try
        {
            // OnlyOffice config requested
            
            var fileEntity = await _fileService.GetFileByIdAsync(id);
            
            if (fileEntity == null)
            {
                // File not found
                return NotFound(new { message = "File not found" });
            }

            // File found

            if (!System.IO.File.Exists(fileEntity.FilePath))
            {
                // File not found on disk
                return NotFound(new { message = "File not found on disk" });
            }

            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            // Base URL determined
            
            var config = new
            {
                document = new
                {
                    fileType = GetFileExtension(fileEntity.OriginalName),
                    key = $"file-{fileEntity.Id}-{DateTime.UtcNow:yyyyMMddHHmmssffff}",
                    title = fileEntity.OriginalName,
                    url = $"{baseUrl}/api/onlyoffice/download/{fileEntity.Id}",
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

            // Config generated successfully

            return Ok(config);
        }
        catch (Exception ex)
        {
            // Config generation failed
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