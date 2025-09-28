using OnlyOfficeServer.Models;
using OnlyOfficeServer.Repositories;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using System.IO;
using Microsoft.EntityFrameworkCore;
using OnlyOfficeServer.Data;

namespace OnlyOfficeServer.Managers;

public class OnlyOfficeManager
{
    private readonly IOnlyOfficeRepository _repository;
    private readonly IConfiguration _configuration;
    private readonly AppDbContext _context;
    private readonly IInstallationRepository _installationRepository;

    public OnlyOfficeManager(IOnlyOfficeRepository repository, IConfiguration configuration, AppDbContext context, IInstallationRepository installationRepository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _installationRepository = installationRepository ?? throw new ArgumentNullException(nameof(installationRepository));
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

        // Use configured host URL for Docker compatibility, fallback to baseUrl
        var hostUrl = _configuration["OnlyOffice:HostUrl"] ?? baseUrl;
        var documentServerUrl = _configuration["OnlyOffice:DocumentServerUrl"];

        // Business logic: Build OnlyOffice configuration
        var config = new OnlyOfficeConfigResult
        {
            Document = new DocumentConfig
            {
                FileType = GetFileExtension(fileEntity.OriginalName),
                Key = GenerateDocumentKey(fileEntity),
                Title = fileEntity.OriginalName,
                Url = $"{hostUrl}/api/onlyoffice/download/{fileEntity.Id}",
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
            },
            OnlyOfficeServerUrl = documentServerUrl
        };

        // Generate JWT token for the complete config
        config.Token = GenerateJwtToken(config);

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


    // JWT generation using Newtonsoft.Json
    private string GenerateJwtToken(OnlyOfficeConfigResult config)
    {
        var jwtSecret = _configuration["OnlyOffice:JwtSecret"];
        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new InvalidOperationException("OnlyOffice JWT secret not configured");
        }

        // Create JWT payload (same as what was done in Angular)
        var payload = new
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
            }
        };

        return CreateJwt(payload, jwtSecret);
    }

    private string CreateJwt(object payload, string secret)
    {
        // JWT Header
        var header = new { alg = "HS256", typ = "JWT" };

        // Create JsonSerializerSettings
        var jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Formatting = Formatting.None
        };

        // Encode header and payload using Newtonsoft.Json
        var headerJson = JsonConvert.SerializeObject(header, jsonSettings);
        var payloadJson = JsonConvert.SerializeObject(payload, jsonSettings);
        var encodedHeader = Base64UrlEncode(headerJson);
        var encodedPayload = Base64UrlEncode(payloadJson);
        var message = $"{encodedHeader}.{encodedPayload}";
        

        // Create signature using HMACSHA256
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            var base64Signature = Convert.ToBase64String(signatureBytes);
            var encodedSignature = base64Signature.Replace('+', '-').Replace('/', '_').Replace("=", "");

            return $"{message}.{encodedSignature}";
        }
    }

    private string Base64UrlEncode(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var base64 = Convert.ToBase64String(bytes);
        return base64.Replace('+', '-').Replace('/', '_').Replace("=", "");
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

    // **Installation methods using AppDbContext (Approach 2)**
    public async Task<string> GetApplicationUrlAsync(int applicationId)
    {
        var installation = await _context.Installations
            .FirstOrDefaultAsync(i => i.ApplicationId == applicationId);

        if (installation == null)
        {
            throw new InvalidOperationException($"Installation with ApplicationId '{applicationId}' not found in database");
        }

        return installation.FullUrl;
    }

    public async Task<Installation?> GetInstallationByApplicationIdAsync(int applicationId)
    {
        return await _context.Installations
            .FirstOrDefaultAsync(i => i.ApplicationId == applicationId);
    }

    // **Installation methods using InstallationRepository (Approach 3)**
    public async Task<string> GetApplicationUrlViaRepositoryAsync(int applicationId)
    {
        var installation = await _installationRepository.GetByApplicationIdAsync(applicationId);

        if (installation == null)
        {
            throw new InvalidOperationException($"Installation with ApplicationId '{applicationId}' not found in database");
        }

        return installation.FullUrl;
    }

    public async Task<Installation?> GetInstallationByApplicationIdViaRepositoryAsync(int applicationId)
    {
        return await _installationRepository.GetByApplicationIdAsync(applicationId);
    }

}

// DTOs for business layer
public class OnlyOfficeConfigResult
{
    public DocumentConfig Document { get; set; } = null!;
    public string DocumentType { get; set; } = string.Empty;
    public EditorConfig EditorConfig { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
    public string OnlyOfficeServerUrl { get; set; } = string.Empty;
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