using OnlyOfficeServer.Models;
using OnlyOfficeServer.Repositories;

namespace OnlyOfficeServer.Managers;

public class OnlyOfficeManager
{
    private readonly IOnlyOfficeRepository _repository;

    public OnlyOfficeManager(IOnlyOfficeRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<OnlyOfficeConfigResult> GetConfigAsync(int fileId, string baseUrl)
    {
        // Business logic: Get file and validate
        var fileEntity = await _repository.GetFileByIdAsync(fileId);
        
        if (fileEntity == null)
        {
            throw new FileNotFoundException($"File with ID {fileId} not found");
        }

        if (!File.Exists(fileEntity.FilePath))
        {
            throw new FileNotFoundException($"Physical file not found: {fileEntity.FilePath}");
        }

        // Business logic: Build OnlyOffice configuration
        var config = new OnlyOfficeConfigResult
        {
            Document = new DocumentConfig
            {
                FileType = GetFileExtension(fileEntity.OriginalName),
                Key = GenerateDocumentKey(fileEntity),
                Title = fileEntity.OriginalName,
                Url = $"{baseUrl}/api/onlyoffice/download/{fileEntity.Id}",
                Permissions = new PermissionsConfig
                {
                    Edit = true,
                    Download = true,
                    Print = true
                }
            },
            DocumentType = GetDocumentType(fileEntity.OriginalName),
            EditorConfig = new EditorConfig
            {
                Mode = "edit"
            }
        };

        return config;
    }

    public async Task<FileDownloadResult> GetFileForDownloadAsync(int fileId)
    {
        // Business logic: Get file and validate
        var fileEntity = await _repository.GetFileByIdAsync(fileId);
        
        if (fileEntity == null)
        {
            throw new FileNotFoundException($"File with ID {fileId} not found");
        }

        if (!File.Exists(fileEntity.FilePath))
        {
            throw new FileNotFoundException($"Physical file not found: {fileEntity.FilePath}");
        }

        // Business logic: Prepare file for download
        var fileBytes = await File.ReadAllBytesAsync(fileEntity.FilePath);
        var contentType = GetContentType(fileEntity.OriginalName);

        return new FileDownloadResult
        {
            Content = fileBytes,
            ContentType = contentType,
            FileName = fileEntity.OriginalName
        };
    }

    // Business logic helper methods
    private string GenerateDocumentKey(FileEntity fileEntity)
    {
        // Generate unique key for OnlyOffice document versioning
        return $"file-{fileEntity.Id}-{DateTime.UtcNow:yyyyMMddHHmmssffff}";
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

// DTOs for business layer (similar to what you'd have in .NET Framework)
public class OnlyOfficeConfigResult
{
    public DocumentConfig Document { get; set; } = null!;
    public string DocumentType { get; set; } = string.Empty;
    public EditorConfig EditorConfig { get; set; } = null!;
}

public class DocumentConfig
{
    public string FileType { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public PermissionsConfig Permissions { get; set; } = null!;
}

public class PermissionsConfig
{
    public bool Edit { get; set; }
    public bool Download { get; set; }
    public bool Print { get; set; }
}

public class EditorConfig
{
    public string Mode { get; set; } = string.Empty;
}

public class FileDownloadResult
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}