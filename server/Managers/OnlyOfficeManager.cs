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

    public OnlyOfficeManager(IOnlyOfficeRepository repository, IConfiguration configuration, AppDbContext context)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<OnlyOfficeConfigResult> GetConfigAsync(Guid fileId, string baseUrl, Guid userId)
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

        // Get user information
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new FileNotFoundException($"User with ID {userId} not found");
        }

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
                CallbackUrl = $"{hostUrl}/api/onlyoffice/callback/{fileEntity.Id}",
                User = new UserConfig
                {
                    Id = user.Id.ToString(),
                    Name = user.Username
                }
            },
            OnlyOfficeServerUrl = documentServerUrl ?? string.Empty
        };

        // Generate JWT token for the complete config
        config.Token = GenerateJwtToken(config);

        return config;
    }

    public async Task<FileDownloadResult> GetFileForDownloadAsync(Guid fileId)
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
                callbackUrl = config.EditorConfig.CallbackUrl,
                user = config.EditorConfig.User != null ? new
                {
                    id = config.EditorConfig.User.Id,
                    name = config.EditorConfig.User.Name
                } : null
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

    // Send forcesave command to OnlyOffice Command Service
    public async Task<ForceSaveCommandResult> SendForceSaveCommandAsync(string documentKey)
    {
        var documentServerUrl = _configuration["OnlyOffice:DocumentServerUrl"];
        var jwtSecret = _configuration["OnlyOffice:JwtSecret"];

        if (string.IsNullOrEmpty(documentServerUrl))
        {
            throw new InvalidOperationException("OnlyOffice DocumentServerUrl not configured");
        }

        if (string.IsNullOrEmpty(jwtSecret))
        {
            throw new InvalidOperationException("OnlyOffice JwtSecret not configured");
        }

        // Build command service URL
        var commandUrl = $"{documentServerUrl.TrimEnd('/')}/coauthoring/CommandService.ashx";

        // Create command payload
        var commandPayload = new
        {
            c = "forcesave",
            key = documentKey
        };

        // Generate JWT token for the command
        var token = CreateJwt(commandPayload, jwtSecret);

        // Create request body with token
        var requestBody = new
        {
            c = commandPayload.c,
            key = commandPayload.key,
            token = token
        };

        using (var httpClient = new HttpClient())
        {
            var jsonSettings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            };

            var jsonContent = JsonConvert.SerializeObject(requestBody, jsonSettings);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(commandUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"[FORCESAVE] Command URL: {commandUrl}");
            Console.WriteLine($"[FORCESAVE] Request: {jsonContent}");
            Console.WriteLine($"[FORCESAVE] Response: {responseContent}");

            var result = JsonConvert.DeserializeObject<ForceSaveCommandResult>(responseContent);

            if (result == null)
            {
                return new ForceSaveCommandResult { Error = 1, Message = "Failed to parse response" };
            }

            return result;
        }
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

    public async Task<bool> ProcessCallbackAsync(Guid fileId, CallbackRequest callback)
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
    public UserConfig? User { get; set; }
}

public class UserConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}

public class FileDownloadResult
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public class ForceSaveCommandResult
{
    public int Error { get; set; }
    public string? Key { get; set; }
    public string? Message { get; set; }
}