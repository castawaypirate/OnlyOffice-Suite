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
        // var hostUrl = baseUrl;
        var hostUrl = "http://host.docker.internal:5142"; //For docker
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
                Mode = "edit",
                CallbackUrl = $"{hostUrl}/api/onlyoffice/callback/{fileEntity.Id}"
            },
            OnlyOfficeServerUrl = documentServerUrl ?? string.Empty
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
                mode = config.EditorConfig.Mode,
                callbackUrl = config.EditorConfig.CallbackUrl
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
        // Generate a unique key based on file ID and last modified date
        // This ensures the key changes when the document is edited via OnlyOffice callback
        var keySource = $"file-{fileEntity.Id}-{fileEntity.LastModifiedAt:yyyyMMddHHmmss}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(keySource)).Replace("=", "").Replace("+", "-").Replace("/", "_");
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

    public async Task<bool> ProcessCallbackAsync(int fileId, CallbackRequest callback)
    {
        try
        {
            Console.WriteLine($"[CALLBACK DEBUG] Processing callback for file {fileId}, Status: {callback.Status}");

            // Get the file entity directly from _context so it's tracked by EF Core
            // This is critical - using _repository would fetch from a different context!
            var fileEntity = await _context.Files.FirstOrDefaultAsync(f => f.Id == fileId);
            if (fileEntity == null)
            {
                Console.WriteLine($"[CALLBACK ERROR] File with ID {fileId} not found");
                throw new FileNotFoundException($"File with ID {fileId} not found");
            }

            Console.WriteLine($"[CALLBACK DEBUG] File found: {fileEntity.OriginalName}, LastModifiedAt: {fileEntity.LastModifiedAt:yyyy-MM-dd HH:mm:ss}");

            // Validate the document key matches
            var expectedKey = GenerateDocumentKey(fileEntity);
            Console.WriteLine($"[CALLBACK DEBUG] Expected key: {expectedKey}");
            Console.WriteLine($"[CALLBACK DEBUG] Received key: {callback.Key}");

            if (callback.Key != expectedKey)
            {
                Console.WriteLine($"[CALLBACK ERROR] Document key mismatch! Expected: {expectedKey}, Got: {callback.Key}");
                throw new UnauthorizedAccessException($"Document key mismatch for file {fileId}");
            }

            Console.WriteLine($"[CALLBACK DEBUG] Key validation passed");

            // Handle different status codes
            switch (callback.Status)
            {
                case 1: // Document being edited
                    // Just log or update last access time
                    return true;

                case 2: // Document ready for saving
                    Console.WriteLine($"[CALLBACK DEBUG] Status 2 - Document ready for saving");
                    if (string.IsNullOrEmpty(callback.Url))
                    {
                        Console.WriteLine($"[CALLBACK ERROR] No download URL provided");
                        throw new InvalidOperationException("No download URL provided for saving");
                    }

                    Console.WriteLine($"[CALLBACK DEBUG] Downloading from: {callback.Url}");

                    // Download the edited document
                    using (var httpClient = new HttpClient())
                    {
                        var editedFileBytes = await httpClient.GetByteArrayAsync(callback.Url);
                        Console.WriteLine($"[CALLBACK DEBUG] Downloaded {editedFileBytes.Length} bytes");

                        // Replace the original file with the edited version
                        Console.WriteLine($"[CALLBACK DEBUG] Writing to: {fileEntity.FilePath}");
                        await File.WriteAllBytesAsync(fileEntity.FilePath, editedFileBytes);
                        Console.WriteLine($"[CALLBACK DEBUG] File written successfully");

                        // Update LastModifiedAt to change the document key for next edit session
                        var oldModifiedAt = fileEntity.LastModifiedAt;
                        fileEntity.LastModifiedAt = DateTime.UtcNow;
                        Console.WriteLine($"[CALLBACK DEBUG] Updating LastModifiedAt from {oldModifiedAt:yyyy-MM-dd HH:mm:ss} to {fileEntity.LastModifiedAt:yyyy-MM-dd HH:mm:ss}");

                        await _context.SaveChangesAsync();
                        Console.WriteLine($"[CALLBACK DEBUG] Database updated successfully");
                    }

                    return true;

                case 3: // Document saving error
                    // Log the error but don't fail
                    return true;

                case 4: // Document closed without changes
                    // Nothing to do
                    return true;

                case 6: // Document force saved
                    Console.WriteLine($"[CALLBACK DEBUG] Status 6 - Document force saved");
                    if (!string.IsNullOrEmpty(callback.Url))
                    {
                        Console.WriteLine($"[CALLBACK DEBUG] Downloading from: {callback.Url}");
                        using (var httpClient = new HttpClient())
                        {
                            var editedFileBytes = await httpClient.GetByteArrayAsync(callback.Url);
                            Console.WriteLine($"[CALLBACK DEBUG] Downloaded {editedFileBytes.Length} bytes");

                            Console.WriteLine($"[CALLBACK DEBUG] Writing to: {fileEntity.FilePath}");
                            await File.WriteAllBytesAsync(fileEntity.FilePath, editedFileBytes);
                            Console.WriteLine($"[CALLBACK DEBUG] File written successfully");

                            // Update LastModifiedAt to change the document key for next edit session
                            var oldModifiedAt = fileEntity.LastModifiedAt;
                            fileEntity.LastModifiedAt = DateTime.UtcNow;
                            Console.WriteLine($"[CALLBACK DEBUG] Updating LastModifiedAt from {oldModifiedAt:yyyy-MM-dd HH:mm:ss} to {fileEntity.LastModifiedAt:yyyy-MM-dd HH:mm:ss}");

                            await _context.SaveChangesAsync();
                            Console.WriteLine($"[CALLBACK DEBUG] Database updated successfully");
                        }
                    }
                    else
                    {
                        Console.WriteLine($"[CALLBACK DEBUG] No URL provided for status 6");
                    }
                    return true;

                case 7: // Force save error
                    // Log the error but don't fail
                    return true;

                default:
                    Console.WriteLine($"[CALLBACK DEBUG] Unknown status: {callback.Status}");
                    // Unknown status, log but return success
                    return true;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CALLBACK ERROR] Exception: {ex.GetType().Name} - {ex.Message}");
            Console.WriteLine($"[CALLBACK ERROR] StackTrace: {ex.StackTrace}");
            // Re-throw to be handled by controller
            throw;
        }
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
    public string? CallbackUrl { get; set; }
}

public class FileDownloadResult
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}