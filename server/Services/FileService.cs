using Microsoft.EntityFrameworkCore;
using OnlyOfficeServer.Data;
using OnlyOfficeServer.Models;

namespace OnlyOfficeServer.Services;

public class FileService
{
    private readonly AppDbContext _context;
    private readonly string _uploadsPath;

    public FileService(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _uploadsPath = Path.Combine(environment.ContentRootPath, "uploads");
    }

    public async Task<FileEntity> SaveFileAsync(IFormFile file, int userId)
    {
        // Create user directory if it doesn't exist
        var userDir = Path.Combine(_uploadsPath, userId.ToString());
        Directory.CreateDirectory(userDir);

        // Generate unique filename to avoid collisions
        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(userDir, fileName);

        // Save file to disk
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Create database record
        var fileEntity = new FileEntity
        {
            UserId = userId,
            Filename = fileName,
            OriginalName = file.FileName,
            FilePath = filePath,
            Token = GenerateFileToken(),
            TokenExpires = DateTime.UtcNow.AddDays(30), // 30 days expiration
            UploadedAt = DateTime.UtcNow
        };

        _context.Files.Add(fileEntity);
        await _context.SaveChangesAsync();

        return fileEntity;
    }

    public async Task<List<FileEntity>> GetUserFilesAsync(int userId)
    {
        return await _context.Files
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();
    }

    public async Task<FileEntity?> GetFileByIdAsync(int fileId, int userId)
    {
        return await _context.Files
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);
    }

    public async Task<FileEntity?> GetFileByTokenAsync(string token)
    {
        return await _context.Files
            .Include(f => f.User)
            .FirstOrDefaultAsync(f => f.Token == token && f.TokenExpires > DateTime.UtcNow);
    }

    private string GenerateFileToken()
    {
        return Guid.NewGuid().ToString("N")[..32]; // 32 character token
    }

    public async Task<bool> DeleteFileAsync(int fileId, int userId)
    {
        var fileEntity = await GetFileByIdAsync(fileId, userId);
        if (fileEntity == null)
            return false;

        // Delete file from disk
        if (File.Exists(fileEntity.FilePath))
        {
            File.Delete(fileEntity.FilePath);
        }

        // Delete from database
        _context.Files.Remove(fileEntity);
        await _context.SaveChangesAsync();

        return true;
    }

    public string GetFileSize(string filePath)
    {
        if (!File.Exists(filePath))
            return "0 bytes";

        var fileInfo = new FileInfo(filePath);
        var size = fileInfo.Length;

        if (size < 1024)
            return $"{size} bytes";
        else if (size < 1024 * 1024)
            return $"{size / 1024:F1} KB";
        else if (size < 1024 * 1024 * 1024)
            return $"{size / (1024 * 1024):F1} MB";
        else
            return $"{size / (1024 * 1024 * 1024):F1} GB";
    }
}