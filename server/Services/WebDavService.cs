using System.Text;
using System.Xml;
using Microsoft.EntityFrameworkCore;
using OnlyOfficeServer.Data;
using OnlyOfficeServer.Models;

namespace OnlyOfficeServer.Services;

public class WebDavService
{
    private readonly AppDbContext _context;
    private readonly FileService _fileService;
    private readonly string _uploadsPath;

    public WebDavService(AppDbContext context, FileService fileService, IWebHostEnvironment environment)
    {
        _context = context;
        _fileService = fileService;
        _uploadsPath = Path.Combine(environment.ContentRootPath, "uploads");
    }

    public async Task<User?> AuthenticateUserAsync(string username, string password)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username && u.Password == password);
    }

    public async Task<List<FileEntity>> GetUserFilesAsync(int userId)
    {
        return await _fileService.GetUserFilesAsync(userId);
    }

    public async Task<FileEntity?> GetFileByNameAsync(int userId, string fileName)
    {
        return await _context.Files
            .FirstOrDefaultAsync(f => f.UserId == userId && f.OriginalName == fileName);
    }

    public string GeneratePropfindResponse(List<FileEntity> files, string baseUrl, int userId)
    {
        var xml = new StringBuilder();
        xml.AppendLine("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        xml.AppendLine("<D:multistatus xmlns:D=\"DAV:\">");

        // Add the collection (directory) itself
        xml.AppendLine("  <D:response>");
        xml.AppendLine($"    <D:href>{baseUrl}/webdav/{userId}/</D:href>");
        xml.AppendLine("    <D:propstat>");
        xml.AppendLine("      <D:prop>");
        xml.AppendLine("        <D:resourcetype><D:collection/></D:resourcetype>");
        xml.AppendLine("        <D:displayname>Documents</D:displayname>");
        xml.AppendLine($"        <D:getlastmodified>{DateTime.UtcNow:R}</D:getlastmodified>");
        xml.AppendLine("      </D:prop>");
        xml.AppendLine("      <D:status>HTTP/1.1 200 OK</D:status>");
        xml.AppendLine("    </D:propstat>");
        xml.AppendLine("  </D:response>");

        // Add each file
        foreach (var file in files)
        {
            xml.AppendLine("  <D:response>");
            xml.AppendLine($"    <D:href>{baseUrl}/webdav/{userId}/{Uri.EscapeDataString(file.OriginalName)}</D:href>");
            xml.AppendLine("    <D:propstat>");
            xml.AppendLine("      <D:prop>");
            xml.AppendLine("        <D:resourcetype/>");
            xml.AppendLine($"        <D:displayname>{XmlEscape(file.OriginalName)}</D:displayname>");
            xml.AppendLine($"        <D:getcontentlength>{GetFileLength(file.FilePath)}</D:getcontentlength>");
            xml.AppendLine($"        <D:getlastmodified>{file.UploadedAt:R}</D:getlastmodified>");
            xml.AppendLine($"        <D:getcontenttype>{GetContentType(file.OriginalName)}</D:getcontenttype>");
            xml.AppendLine("      </D:prop>");
            xml.AppendLine("      <D:status>HTTP/1.1 200 OK</D:status>");
            xml.AppendLine("    </D:propstat>");
            xml.AppendLine("  </D:response>");
        }

        xml.AppendLine("</D:multistatus>");
        return xml.ToString();
    }

    public async Task<FileEntity> SaveFileAsync(int userId, string fileName, Stream fileStream)
    {
        // Create user directory if it doesn't exist
        var userDir = Path.Combine(_uploadsPath, userId.ToString());
        Directory.CreateDirectory(userDir);

        // Check if file already exists and delete it
        var existingFile = await GetFileByNameAsync(userId, fileName);
        if (existingFile != null)
        {
            if (File.Exists(existingFile.FilePath))
            {
                File.Delete(existingFile.FilePath);
            }
            _context.Files.Remove(existingFile);
        }

        // Generate unique filename to avoid collisions but keep original name reference
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var filePath = Path.Combine(userDir, uniqueFileName);

        // Save file to disk
        using (var diskStream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(diskStream);
        }

        // Create database record
        var fileEntity = new FileEntity
        {
            UserId = userId,
            Filename = uniqueFileName,
            OriginalName = fileName,
            FilePath = filePath,
            Token = Guid.NewGuid().ToString("N")[..32],
            TokenExpires = DateTime.UtcNow.AddDays(30),
            UploadedAt = DateTime.UtcNow
        };

        _context.Files.Add(fileEntity);
        await _context.SaveChangesAsync();

        return fileEntity;
    }

    public async Task<bool> DeleteFileAsync(int userId, string fileName)
    {
        var file = await GetFileByNameAsync(userId, fileName);
        if (file == null)
            return false;

        // Delete file from disk
        if (File.Exists(file.FilePath))
        {
            File.Delete(file.FilePath);
        }

        // Delete from database
        _context.Files.Remove(file);
        await _context.SaveChangesAsync();

        return true;
    }

    private long GetFileLength(string filePath)
    {
        if (!File.Exists(filePath))
            return 0;
        
        var fileInfo = new FileInfo(filePath);
        return fileInfo.Length;
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

    private string XmlEscape(string text)
    {
        return text.Replace("&", "&amp;")
                  .Replace("<", "&lt;")
                  .Replace(">", "&gt;")
                  .Replace("\"", "&quot;")
                  .Replace("'", "&apos;");
    }
}