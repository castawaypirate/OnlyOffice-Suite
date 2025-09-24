using OnlyOfficeServer.Models;
using OnlyOfficeServer.Repositories;
using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.IO;

namespace OnlyOfficeServer.Managers;

public class OnlyOfficeManager
{
    private readonly IOnlyOfficeRepository _repository;
    private readonly IConfiguration _configuration;

    public OnlyOfficeManager(IOnlyOfficeRepository repository, IConfiguration configuration)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
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

    // Option 2: Hardcoded JSON method - Force exact TypeScript match
    public async Task<OnlyOfficeConfigResult> GetConfigWithHardcodedJsonAsync(int fileId, string baseUrl)
    {
        var fileEntity = await _repository.GetFileByIdAsync(fileId);
        
        if (fileEntity == null)
            throw new FileNotFoundException($"File with ID {fileId} not found");
        if (!File.Exists(fileEntity.FilePath))
            throw new FileNotFoundException($"Physical file not found: {fileEntity.FilePath}");

        var config = new OnlyOfficeConfigResult
        {
            Document = new DocumentConfig
            {
                FileType = GetFileExtension(fileEntity.OriginalName),
                Key = GenerateDocumentKey(fileEntity),
                Title = fileEntity.OriginalName,
                Url = $"{baseUrl}/api/onlyoffice/download/{fileEntity.Id}",
                Permissions = new PermissionsConfig { Edit = true, Download = true, Print = true }
            },
            DocumentType = GetDocumentType(fileEntity.OriginalName),
            EditorConfig = new EditorConfig { Mode = "edit" }
        };

        // Generate JWT with hardcoded JSON strings
        config.Token = GenerateJwtWithHardcodedJson(config);
        return config;
    }

    // Option 5: TypeScript-style method - Mimic JavaScript behavior
    public async Task<OnlyOfficeConfigResult> GetConfigTypeScriptStyleAsync(int fileId, string baseUrl)
    {
        var fileEntity = await _repository.GetFileByIdAsync(fileId);
        
        if (fileEntity == null)
            throw new FileNotFoundException($"File with ID {fileId} not found");
        if (!File.Exists(fileEntity.FilePath))
            throw new FileNotFoundException($"Physical file not found: {fileEntity.FilePath}");

        var config = new OnlyOfficeConfigResult
        {
            Document = new DocumentConfig
            {
                FileType = GetFileExtension(fileEntity.OriginalName),
                Key = GenerateDocumentKey(fileEntity),
                Title = fileEntity.OriginalName,
                Url = $"{baseUrl}/api/onlyoffice/download/{fileEntity.Id}",
                Permissions = new PermissionsConfig { Edit = true, Download = true, Print = true }
            },
            DocumentType = GetDocumentType(fileEntity.OriginalName),
            EditorConfig = new EditorConfig { Mode = "edit" }
        };

        // Generate JWT using TypeScript-style implementation
        config.Token = GenerateJwtTypeScriptStyle(config);
        return config;
    }

    // Alternative 1: Manual JWT method - No JSON library dependencies  
    public async Task<OnlyOfficeConfigResult> GetConfigManualJwtAsync(int fileId, string baseUrl)
    {
        var fileEntity = await _repository.GetFileByIdAsync(fileId);
        
        if (fileEntity == null)
            throw new FileNotFoundException($"File with ID {fileId} not found");
        if (!File.Exists(fileEntity.FilePath))
            throw new FileNotFoundException($"Physical file not found: {fileEntity.FilePath}");

        var config = new OnlyOfficeConfigResult
        {
            Document = new DocumentConfig
            {
                FileType = GetFileExtension(fileEntity.OriginalName),
                Key = GenerateDocumentKey(fileEntity),
                Title = fileEntity.OriginalName,
                Url = $"{baseUrl}/api/onlyoffice/download/{fileEntity.Id}",
                Permissions = new PermissionsConfig { Edit = true, Download = true, Print = true }
            },
            DocumentType = GetDocumentType(fileEntity.OriginalName),
            EditorConfig = new EditorConfig { Mode = "edit" }
        };

        // Generate JWT completely manually - no JSON library
        config.Token = GenerateJwtManually(config);
        return config;
    }



    // Method 3: DataContractJsonSerializer method
    public async Task<OnlyOfficeConfigResult> GetConfigDataContractAsync(int fileId, string baseUrl)
    {
        var fileEntity = await _repository.GetFileByIdAsync(fileId);
        
        if (fileEntity == null)
            throw new FileNotFoundException($"File with ID {fileId} not found");
        if (!File.Exists(fileEntity.FilePath))
            throw new FileNotFoundException($"Physical file not found: {fileEntity.FilePath}");

        var config = new OnlyOfficeConfigResult
        {
            Document = new DocumentConfig
            {
                FileType = GetFileExtension(fileEntity.OriginalName),
                Key = GenerateDocumentKey(fileEntity),
                Title = fileEntity.OriginalName,
                Url = $"{baseUrl}/api/onlyoffice/download/{fileEntity.Id}",
                Permissions = new PermissionsConfig { Edit = true, Download = true, Print = true }
            },
            DocumentType = GetDocumentType(fileEntity.OriginalName),
            EditorConfig = new EditorConfig { Mode = "edit" }
        };

        // Generate JWT using DataContractJsonSerializer
        config.Token = GenerateJwtDataContract(config);
        return config;
    }

    // JWT generation using .NET Framework 4.5.6 compatible methods with Newtonsoft.Json
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

        // Create JsonSerializerSettings for .NET Framework 4.5.6 compatibility
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
        
        // Debug logging to compare with frontend
        Console.WriteLine($"🔍 JWT Debug - Header JSON: {headerJson}");
        Console.WriteLine($"🔍 JWT Debug - Payload JSON: {payloadJson}");
        Console.WriteLine($"🔍 JWT Debug - Header Base64: {encodedHeader}");
        Console.WriteLine($"🔍 JWT Debug - Payload Base64: {encodedPayload}");
        Console.WriteLine($"🔍 JWT Debug - Message to sign: {message}");

        // Create signature using HMACSHA256 (.NET Framework 4.5.6 compatible)
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
        {
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            var base64Signature = Convert.ToBase64String(signatureBytes);
            var encodedSignature = base64Signature.Replace('+', '-').Replace('/', '_').Replace("=", "");
            
            // Debug signature creation
            Console.WriteLine($"🔍 JWT Debug - Raw signature bytes: {Convert.ToHexString(signatureBytes)}");
            Console.WriteLine($"🔍 JWT Debug - Base64 signature: {base64Signature}");
            Console.WriteLine($"🔍 JWT Debug - Base64Url signature: {encodedSignature}");
            Console.WriteLine($"🔍 JWT Debug - Final JWT: {message}.{encodedSignature}");
            
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

    // Option 2: Hardcoded JSON implementation
    private string GenerateJwtWithHardcodedJson(OnlyOfficeConfigResult config)
    {
        var jwtSecret = _configuration["OnlyOffice:JwtSecret"];
        if (string.IsNullOrEmpty(jwtSecret))
            throw new InvalidOperationException("OnlyOffice JWT secret not configured");

        // Hardcode JSON strings to match TypeScript exactly
        var headerJson = "{\"alg\":\"HS256\",\"typ\":\"JWT\"}";
        
        // Build payload JSON manually to ensure exact match
        var payloadJson = BuildPayloadJsonManually(config);
        
        Console.WriteLine($"🔍 HARDCODED - Header JSON: {headerJson}");
        Console.WriteLine($"🔍 HARDCODED - Payload JSON: {payloadJson}");
        
        var encodedHeader = Base64UrlEncode(headerJson);
        var encodedPayload = Base64UrlEncode(payloadJson);
        var message = $"{encodedHeader}.{encodedPayload}";
        
        Console.WriteLine($"🔍 HARDCODED - Message: {message}");
        
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(jwtSecret)))
        {
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            var base64Signature = Convert.ToBase64String(signatureBytes);
            var encodedSignature = base64Signature.Replace('+', '-').Replace('/', '_').Replace("=", "");
            
            Console.WriteLine($"🔍 HARDCODED - Signature starts with: {encodedSignature.Substring(0, 2)}");
            return $"{message}.{encodedSignature}";
        }
    }

    private string BuildPayloadJsonManually(OnlyOfficeConfigResult config)
    {
        // Build JSON string manually to ensure exact property order and formatting
        return $"{{\"document\":{{\"fileType\":\"{config.Document.FileType}\"," +
               $"\"key\":\"{config.Document.Key}\"," +
               $"\"title\":\"{EscapeJsonString(config.Document.Title)}\"," +
               $"\"url\":\"{config.Document.Url}\"," +
               $"\"permissions\":{{\"edit\":{config.Document.Permissions.Edit.ToString().ToLower()}," +
               $"\"download\":{config.Document.Permissions.Download.ToString().ToLower()}," +
               $"\"print\":{config.Document.Permissions.Print.ToString().ToLower()}}}}}," +
               $"\"documentType\":\"{config.DocumentType}\"," +
               $"\"editorConfig\":{{\"mode\":\"{config.EditorConfig.Mode}\"}}}}";
    }

    private string EscapeJsonString(string input)
    {
        // Basic JSON string escaping
        return input.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r");
    }

    // Option 5: TypeScript-style implementation
    private string GenerateJwtTypeScriptStyle(OnlyOfficeConfigResult config)
    {
        var jwtSecret = _configuration["OnlyOffice:JwtSecret"];
        if (string.IsNullOrEmpty(jwtSecret))
            throw new InvalidOperationException("OnlyOffice JWT secret not configured");

        // Mimic TypeScript's JSON.stringify behavior exactly
        var headerJson = TypeScriptJsonStringify(new { alg = "HS256", typ = "JWT" });
        var payloadJson = TypeScriptJsonStringify(new {
            document = new {
                fileType = config.Document.FileType,
                key = config.Document.Key,
                title = config.Document.Title,
                url = config.Document.Url,
                permissions = new {
                    edit = config.Document.Permissions.Edit,
                    download = config.Document.Permissions.Download,
                    print = config.Document.Permissions.Print
                }
            },
            documentType = config.DocumentType,
            editorConfig = new {
                mode = config.EditorConfig.Mode
            }
        });

        Console.WriteLine($"🔍 TYPESCRIPT - Header JSON: {headerJson}");
        Console.WriteLine($"🔍 TYPESCRIPT - Payload JSON: {payloadJson}");

        // Use the SAME Base64Url encoding as the other methods
        var encodedHeader = Base64UrlEncode(headerJson);
        var encodedPayload = Base64UrlEncode(payloadJson);
        var message = $"{encodedHeader}.{encodedPayload}";

        Console.WriteLine($"🔍 TYPESCRIPT - Message: {message}");

        // Use the same HMAC and encoding as all other methods
        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(jwtSecret)))
        {
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            var base64Signature = Convert.ToBase64String(signatureBytes);
            var encodedSignature = base64Signature.Replace('+', '-').Replace('/', '_').Replace("=", "");
            
            Console.WriteLine($"🔍 TYPESCRIPT - Signature starts with: {encodedSignature.Substring(0, 2)}");
            return $"{message}.{encodedSignature}";
        }
    }

    private string TypeScriptJsonStringify(object obj)
    {
        // Match JavaScript JSON.stringify() exactly - minimal settings
        var settings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Culture = System.Globalization.CultureInfo.InvariantCulture,
            // Keep default contract resolver to maintain property names
            ContractResolver = new Newtonsoft.Json.Serialization.DefaultContractResolver()
        };
        
        return JsonConvert.SerializeObject(obj, settings);
    }

    private string BtoaEquivalent(string input)
    {
        // JavaScript btoa() equivalent - string to base64
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes);
    }

    private string BtoaEquivalent(byte[] bytes)
    {
        // JavaScript btoa() equivalent - bytes to base64
        return Convert.ToBase64String(bytes);
    }

    // Alternative 1: Manual JWT generation - No JSON library dependencies
    private string GenerateJwtManually(OnlyOfficeConfigResult config)
    {
        var jwtSecret = _configuration["OnlyOffice:JwtSecret"];
        if (string.IsNullOrEmpty(jwtSecret))
            throw new InvalidOperationException("OnlyOffice JWT secret not configured");

        // Build JSON manually with StringBuilder for exact control
        var header = new StringBuilder();
        header.Append("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");

        var payload = new StringBuilder();
        payload.Append("{\"document\":{");
        payload.Append($"\"fileType\":\"{config.Document.FileType}\",");
        payload.Append($"\"key\":\"{config.Document.Key}\",");
        payload.Append($"\"title\":\"{EscapeJsonString(config.Document.Title)}\",");
        payload.Append($"\"url\":\"{config.Document.Url}\",");
        payload.Append("\"permissions\":{");
        payload.Append($"\"edit\":{config.Document.Permissions.Edit.ToString().ToLower()},");
        payload.Append($"\"download\":{config.Document.Permissions.Download.ToString().ToLower()},");
        payload.Append($"\"print\":{config.Document.Permissions.Print.ToString().ToLower()}");
        payload.Append("}},");
        payload.Append($"\"documentType\":\"{config.DocumentType}\",");
        payload.Append("\"editorConfig\":{");
        payload.Append($"\"mode\":\"{config.EditorConfig.Mode}\"");
        payload.Append("}}");

        var headerJson = header.ToString();
        var payloadJson = payload.ToString();

        Console.WriteLine($"🔍 MANUAL - Header JSON: {headerJson}");
        Console.WriteLine($"🔍 MANUAL - Payload JSON: {payloadJson}");

        var encodedHeader = Base64UrlEncode(headerJson);
        var encodedPayload = Base64UrlEncode(payloadJson);
        var message = $"{encodedHeader}.{encodedPayload}";

        Console.WriteLine($"🔍 MANUAL - Message: {message}");

        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(jwtSecret)))
        {
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            var base64Signature = Convert.ToBase64String(signatureBytes);
            var encodedSignature = base64Signature.Replace('+', '-').Replace('/', '_').Replace("=", "");
            
            Console.WriteLine($"🔍 MANUAL - Signature starts with: {encodedSignature.Substring(0, 2)}");
            return $"{message}.{encodedSignature}";
        }
    }



    // Method 3: DataContractJsonSerializer JWT generation
    private string GenerateJwtDataContract(OnlyOfficeConfigResult config)
    {
        var jwtSecret = _configuration["OnlyOffice:JwtSecret"];
        if (string.IsNullOrEmpty(jwtSecret))
            throw new InvalidOperationException("OnlyOffice JWT secret not configured");

        // Create DataContract objects
        var header = new DataContractJwtHeader { alg = "HS256", typ = "JWT" };
        var payload = new DataContractJwtPayload
        {
            document = new DataContractDocument
            {
                fileType = config.Document.FileType,
                key = config.Document.Key,
                title = config.Document.Title,
                url = config.Document.Url,
                permissions = new DataContractPermissions
                {
                    edit = config.Document.Permissions.Edit,
                    download = config.Document.Permissions.Download,
                    print = config.Document.Permissions.Print
                }
            },
            documentType = config.DocumentType,
            editorConfig = new DataContractEditorConfig { mode = config.EditorConfig.Mode }
        };

        // Serialize using DataContractJsonSerializer
        var headerJson = SerializeDataContract(header);
        var payloadJson = SerializeDataContract(payload);

        Console.WriteLine($"🔍 DATACONTRACT - Header JSON: {headerJson}");
        Console.WriteLine($"🔍 DATACONTRACT - Payload JSON: {payloadJson}");

        var encodedHeader = Base64UrlEncode(headerJson);
        var encodedPayload = Base64UrlEncode(payloadJson);
        var message = $"{encodedHeader}.{encodedPayload}";

        Console.WriteLine($"🔍 DATACONTRACT - Message: {message}");

        using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(jwtSecret)))
        {
            var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
            var base64Signature = Convert.ToBase64String(signatureBytes);
            var encodedSignature = base64Signature.Replace('+', '-').Replace('/', '_').Replace("=", "");
            
            Console.WriteLine($"🔍 DATACONTRACT - Signature starts with: {encodedSignature.Substring(0, 2)}");
            return $"{message}.{encodedSignature}";
        }
    }

    private string SerializeDataContract<T>(T obj)
    {
        var serializer = new DataContractJsonSerializer(typeof(T));
        using (var stream = new MemoryStream())
        {
            serializer.WriteObject(stream, obj);
            return Encoding.UTF8.GetString(stream.ToArray());
        }
    }
}

// DataContract classes for DataContractJsonSerializer
[DataContract]
public class DataContractJwtHeader
{
    [DataMember(Order = 1)]
    public string alg { get; set; } = string.Empty;
    
    [DataMember(Order = 2)]
    public string typ { get; set; } = string.Empty;
}

[DataContract]
public class DataContractJwtPayload
{
    [DataMember(Order = 1)]
    public DataContractDocument document { get; set; } = null!;
    
    [DataMember(Order = 2)]
    public string documentType { get; set; } = string.Empty;
    
    [DataMember(Order = 3)]
    public DataContractEditorConfig editorConfig { get; set; } = null!;
}

[DataContract]
public class DataContractDocument
{
    [DataMember(Order = 1)]
    public string fileType { get; set; } = string.Empty;
    
    [DataMember(Order = 2)]
    public string key { get; set; } = string.Empty;
    
    [DataMember(Order = 3)]
    public string title { get; set; } = string.Empty;
    
    [DataMember(Order = 4)]
    public string url { get; set; } = string.Empty;
    
    [DataMember(Order = 5)]
    public DataContractPermissions permissions { get; set; } = null!;
}

[DataContract]
public class DataContractPermissions
{
    [DataMember(Order = 1)]
    public bool edit { get; set; }
    
    [DataMember(Order = 2)]
    public bool download { get; set; }
    
    [DataMember(Order = 3)]
    public bool print { get; set; }
}

[DataContract]
public class DataContractEditorConfig
{
    [DataMember(Order = 1)]
    public string mode { get; set; } = string.Empty;
}

// DTOs for business layer (similar to what you'd have in .NET Framework)
public class OnlyOfficeConfigResult
{
    public DocumentConfig Document { get; set; } = null!;
    public string DocumentType { get; set; } = string.Empty;
    public EditorConfig EditorConfig { get; set; } = null!;
    public string Token { get; set; } = string.Empty;
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