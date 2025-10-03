using Microsoft.EntityFrameworkCore;
using OnlyOfficeServer.Data;
using OnlyOfficeServer.Models;
using System.Collections.Concurrent;

namespace OnlyOfficeServer.Services;

public class FileService
{
    private readonly AppDbContext _context;
    private readonly string _uploadsPath;
    private readonly string _tempPath;

    // In-memory storage for temp files (in production, consider using Redis or database)
    private static readonly ConcurrentDictionary<string, TempFile> _tempFiles = new();

    public FileService(AppDbContext context, IWebHostEnvironment environment)
    {
        _context = context;
        _uploadsPath = Path.Combine(environment.ContentRootPath, "uploads");
        _tempPath = Path.Combine(environment.ContentRootPath, "uploads", "temp");

        // Ensure temp directory exists
        Directory.CreateDirectory(_tempPath);
    }

    public async Task<FileEntity> SaveFileAsync(IFormFile file, Guid userId)
    {
        // Create user directory if it doesn't exist
        var userDir = Path.Combine(_uploadsPath, userId.ToString());
        Directory.CreateDirectory(userDir);

        // Generate unique filename to avoid collisions
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{Guid.NewGuid()}{extension}";
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
            UploadedAt = DateTime.UtcNow
        };

        _context.Files.Add(fileEntity);
        await _context.SaveChangesAsync();

        return fileEntity;
    }

    public async Task<List<FileEntity>> GetUserFilesAsync(Guid userId)
    {
        return await _context.Files
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();
    }

    public async Task<FileEntity?> GetFileByIdAsync(Guid fileId, Guid userId)
    {
        return await _context.Files
            .FirstOrDefaultAsync(f => f.Id == fileId && f.UserId == userId);
    }

    public async Task<FileEntity?> GetFileByIdAsync(Guid fileId)
    {
        return await _context.Files
            .FirstOrDefaultAsync(f => f.Id == fileId);
    }


    public async Task<bool> DeleteFileAsync(Guid fileId, Guid userId)
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

    // Temp file methods
    public async Task<TempFile> SaveTempFileAsync(IFormFile file, Guid userId)
    {
        // Generate unique temp file ID
        var tempId = Guid.NewGuid().ToString("N");
        var extension = Path.GetExtension(file.FileName);
        var fileName = $"{tempId}{extension}";
        var filePath = Path.Combine(_tempPath, fileName);

        // Save file to temp directory
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Create temp file record
        var tempFile = new TempFile
        {
            Id = tempId,
            UserId = userId,
            OriginalName = file.FileName,
            TempFilePath = filePath,
            Size = file.Length,
            UploadedAt = DateTime.UtcNow
        };

        // Store in memory
        _tempFiles[tempId] = tempFile;

        return tempFile;
    }

    public async Task<FileEntity> SaveTempFileToStorageAsync(string tempId, Guid userId)
    {
        // Get temp file
        if (!_tempFiles.TryGetValue(tempId, out var tempFile) || tempFile.UserId != userId)
        {
            throw new FileNotFoundException("Temp file not found");
        }

        if (!File.Exists(tempFile.TempFilePath))
        {
            throw new FileNotFoundException("Temp file not found on disk");
        }

        // Create user directory if it doesn't exist
        var userDir = Path.Combine(_uploadsPath, userId.ToString());
        Directory.CreateDirectory(userDir);

        // Generate unique filename for permanent storage
        var extension = Path.GetExtension(tempFile.OriginalName);
        var fileName = $"{Guid.NewGuid()}{extension}";
        var permanentPath = Path.Combine(userDir, fileName);

        // Move file from temp to permanent location
        File.Move(tempFile.TempFilePath, permanentPath);

        // Create database record
        var fileEntity = new FileEntity
        {
            UserId = userId,
            Filename = fileName,
            OriginalName = tempFile.OriginalName,
            FilePath = permanentPath,
            UploadedAt = DateTime.UtcNow
        };

        _context.Files.Add(fileEntity);
        await _context.SaveChangesAsync();

        // Remove from temp storage
        _tempFiles.TryRemove(tempId, out _);

        return fileEntity;
    }

    public bool DeleteTempFile(string tempId, Guid userId)
    {
        if (!_tempFiles.TryGetValue(tempId, out var tempFile) || tempFile.UserId != userId)
        {
            return false;
        }

        // Delete file from disk
        if (File.Exists(tempFile.TempFilePath))
        {
            File.Delete(tempFile.TempFilePath);
        }

        // Remove from memory
        _tempFiles.TryRemove(tempId, out _);

        return true;
    }

    public List<TempFile> GetUserTempFiles(Guid userId)
    {
        return _tempFiles.Values
            .Where(tf => tf.UserId == userId)
            .OrderByDescending(tf => tf.UploadedAt)
            .ToList();
    }

    public void CleanupUserTempFiles(Guid userId)
    {
        var userTempFiles = _tempFiles.Values
            .Where(tf => tf.UserId == userId)
            .ToList();

        foreach (var tempFile in userTempFiles)
        {
            // Delete file from disk
            if (File.Exists(tempFile.TempFilePath))
            {
                File.Delete(tempFile.TempFilePath);
            }

            // Remove from memory
            _tempFiles.TryRemove(tempFile.Id, out _);
        }
    }

    public void CleanupOldTempFiles(TimeSpan maxAge)
    {
        var cutoffTime = DateTime.UtcNow - maxAge;
        var oldTempFiles = _tempFiles.Values
            .Where(tf => tf.UploadedAt < cutoffTime)
            .ToList();

        foreach (var tempFile in oldTempFiles)
        {
            // Delete file from disk
            if (File.Exists(tempFile.TempFilePath))
            {
                File.Delete(tempFile.TempFilePath);
            }

            // Remove from memory
            _tempFiles.TryRemove(tempFile.Id, out _);
        }
    }
}