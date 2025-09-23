# OnlyOffice Integration Roadmap for Legacy .NET Framework

This roadmap provides a practical, step-by-step implementation guide for migrating OnlyOffice integration from frontend JWT generation to backend JWT generation in a legacy .NET Framework 4.5.6 application using a divide-and-conquer approach.

## 📊 Current State Analysis

### ✅ **What You Have**
- OnlyOffice download endpoint in DocumentController
- JWT generation implemented in frontend (Angular 17.x.x)
- Basic OnlyOffice Document Server integration
- Manager-Repository pattern in legacy application

### ❌ **Current Problem**
- OnlyOffice Document Server cannot download documents due to authentication requirements
- JWT generation in frontend creates security concerns
- JWT generation compatibility issues with .NET Framework
- No proper separation between user operations and system operations

### 🎯 **Target State**
- Backend JWT generation using .NET Framework 4.5.6 with Newtonsoft.Json
- Clean OnlyOffice-specific endpoints without authentication issues
- Incremental implementation with real application testing
- Clean separation of concerns following existing patterns

---

## 🛠️ Implementation Steps

### **Step 1: Create Basic Backend Infrastructure**

**Goal**: Create OnlyOffice-specific controller and manager with hardcoded configuration

**Backend Implementation**:

**Create OnlyOfficeController.cs**:
```csharp
[ApiController]
[Route("api/[controller]")]
public class OnlyOfficeController : ControllerBase // Or Controller in .NET Framework
{
    [HttpGet("config/{id}")]
    public ActionResult GetConfig(int id) // No async for now
    {
        using (var repo = new OnlyOfficeRepository())
        {
            var manager = new OnlyOfficeManager(repo);
            var config = manager.GetConfig(id);
            return Json(config); // .NET Framework: return Json(config, JsonRequestBehavior.AllowGet);
        }
    }

    [HttpGet("download/{id}")]
    public ActionResult DownloadFile(int id)
    {
        using (var repo = new OnlyOfficeRepository())
        {
            var manager = new OnlyOfficeManager(repo);
            var fileResult = manager.GetFileForDownload(id);
            return File(fileResult.Content, fileResult.ContentType, fileResult.FileName);
        }
    }
}
```

**Create OnlyOfficeManager.cs**:
```csharp
public class OnlyOfficeManager
{
    private readonly IOnlyOfficeRepository _repository;

    public OnlyOfficeManager(IOnlyOfficeRepository repository)
    {
        _repository = repository;
    }

    public object GetConfig(int fileId)
    {
        var file = _repository.GetFileById(fileId);
        
        // HARDCODED configuration for now
        var config = new
        {
            document = new
            {
                fileType = "docx", // HARDCODED
                key = "hardcoded-key-123", // HARDCODED
                title = "test.docx", // HARDCODED
                url = "http://localhost:your-port/api/onlyoffice/download/" + fileId, // HARDCODED base URL
                permissions = new
                {
                    edit = true,
                    download = true,
                    print = true
                }
            },
            documentType = "word", // HARDCODED
            editorConfig = new
            {
                mode = "edit"
            }
        };

        // JWT generation will be added later
        return new
        {
            document = config.document,
            documentType = config.documentType,
            editorConfig = config.editorConfig,
            token = "hardcoded-jwt-token" // HARDCODED for now
        };
    }

    public FileDownloadResult GetFileForDownload(int fileId)
    {
        var file = _repository.GetFileById(fileId);
        var fileBytes = File.ReadAllBytes(file.FilePath);
        
        return new FileDownloadResult
        {
            Content = fileBytes,
            ContentType = "application/octet-stream", // HARDCODED for now
            FileName = file.OriginalName
        };
    }
}
```

**Create OnlyOfficeRepository.cs**:
```csharp
public class OnlyOfficeRepository : IOnlyOfficeRepository, IDisposable
{
    private readonly YourDbContext _context;

    public OnlyOfficeRepository()
    {
        _context = new YourDbContext(); // Your existing context
    }

    public FileEntity GetFileById(int id)
    {
        // No user filtering for OnlyOffice operations
        return _context.Files.FirstOrDefault(f => f.Id == id);
    }

    public void Dispose()
    {
        _context?.Dispose();
    }
}
```

**Frontend Update**: Update Angular service to call new endpoint:
```typescript
getOnlyOfficeConfig(fileId: number): Observable<any> {
  return this.http.get(`${this.apiUrl}/onlyoffice/config/${fileId}`);
}
```

**Frontend Component**: Remove JWT generation, use backend config:
```typescript
ngOnInit() {
  this.fileId = this.route.snapshot.paramMap.get('fileId') || '';
  this.loadFileData();
}

private loadFileData() {
  const fileIdNum = parseInt(this.fileId, 10);
  
  this.fileService.getOnlyOfficeConfig(fileIdNum).subscribe({
    next: (backendConfig) => {
      this.config = backendConfig; // Use complete config from backend
      this.fileName = backendConfig.document.title;
    },
    error: (error) => {
      console.error('Failed to load OnlyOffice config:', error);
    }
  });
}
```

**Testing**: Open document in OnlyOffice component, check browser network tab and OnlyOffice Document Server logs

---

### **Step 2: Add JWT Generation with Newtonsoft.Json**

**Goal**: Replace hardcoded JWT token with actual JWT generation

**Add Newtonsoft.Json Package** (.NET Framework):
```xml
<PackageReference Include="Newtonsoft.Json" Version="13.0.3" />
```

**Update OnlyOfficeManager with JWT generation**:
```csharp
using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

public class OnlyOfficeManager
{
    // ... existing code ...

    public object GetConfig(int fileId)
    {
        var file = _repository.GetFileById(fileId);
        
        var config = new
        {
            document = new
            {
                fileType = "docx", // Still hardcoded
                key = "hardcoded-key-123", // Still hardcoded
                title = "test.docx", // Still hardcoded
                url = "http://localhost:your-port/api/onlyoffice/download/" + fileId,
                permissions = new { edit = true, download = true, print = true }
            },
            documentType = "word", // Still hardcoded
            editorConfig = new { mode = "edit" }
        };

        // Generate real JWT token
        var jwtSecret = "1Z8ezN1VlhBy95axTeD6yIi51PZGGmyk"; // Move to config later
        var token = GenerateJwtToken(config, jwtSecret);

        return new
        {
            document = config.document,
            documentType = config.documentType,
            editorConfig = config.editorConfig,
            token = token // Real JWT token
        };
    }

    private string GenerateJwtToken(object payload, string secret)
    {
        var header = new { alg = "HS256", typ = "JWT" };
        
        var jsonSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            Formatting = Formatting.None
        };

        var encodedHeader = Base64UrlEncode(JsonConvert.SerializeObject(header, jsonSettings));
        var encodedPayload = Base64UrlEncode(JsonConvert.SerializeObject(payload, jsonSettings));
        var message = $"{encodedHeader}.{encodedPayload}";

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
}
```

**Testing**: Test document opening in OnlyOffice. Check OnlyOffice Document Server logs for JWT validation success/failure.

---

### **Step 3: Make URL Dynamic**

**Goal**: Generate base URL dynamically instead of hardcoding

**Update OnlyOfficeManager**:
```csharp
public object GetConfig(int fileId, string baseUrl) // Add baseUrl parameter
{
    var file = _repository.GetFileById(fileId);
    
    var config = new
    {
        document = new
        {
            fileType = "docx", // Still hardcoded
            key = "hardcoded-key-123", // Still hardcoded
            title = "test.docx", // Still hardcoded
            url = $"{baseUrl}/api/onlyoffice/download/{fileId}", // Dynamic URL
            permissions = new { edit = true, download = true, print = true }
        },
        documentType = "word", // Still hardcoded
        editorConfig = new { mode = "edit" }
    };

    var jwtSecret = "1Z8ezN1VlhBy95axTeD6yIi51PZGGmyk";
    var token = GenerateJwtToken(config, jwtSecret);

    return new { document = config.document, documentType = config.documentType, editorConfig = config.editorConfig, token = token };
}
```

**Update OnlyOfficeController**:
```csharp
[HttpGet("config/{id}")]
public ActionResult GetConfig(int id)
{
    using (var repo = new OnlyOfficeRepository())
    {
        var manager = new OnlyOfficeManager(repo);
        var baseUrl = $"{Request.Url.Scheme}://{Request.Url.Authority}"; // .NET Framework
        // For .NET Core: var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var config = manager.GetConfig(id, baseUrl);
        return Json(config, JsonRequestBehavior.AllowGet); // .NET Framework
    }
}
```

**Testing**: Verify URL is correctly generated for your environment

---

### **Step 4: Add Dynamic File Extension**

**Goal**: Detect file extension from actual file instead of hardcoding

**Update OnlyOfficeManager**:
```csharp
public object GetConfig(int fileId, string baseUrl)
{
    var file = _repository.GetFileById(fileId);
    
    var config = new
    {
        document = new
        {
            fileType = GetFileExtension(file.OriginalName), // Dynamic
            key = "hardcoded-key-123", // Still hardcoded
            title = file.OriginalName, // Dynamic
            url = $"{baseUrl}/api/onlyoffice/download/{fileId}",
            permissions = new { edit = true, download = true, print = true }
        },
        documentType = GetDocumentType(file.OriginalName), // Dynamic
        editorConfig = new { mode = "edit" }
    };

    var jwtSecret = "1Z8ezN1VlhBy95axTeD6yIi51PZGGmyk";
    var token = GenerateJwtToken(config, jwtSecret);

    return new { document = config.document, documentType = config.documentType, editorConfig = config.editorConfig, token = token };
}

private string GetFileExtension(string fileName)
{
    return Path.GetExtension(fileName).TrimStart('.').ToLowerInvariant();
}

private string GetDocumentType(string fileName)
{
    var extension = Path.GetExtension(fileName).ToLowerInvariant();
    switch (extension)
    {
        case ".doc":
        case ".docx":
            return "word";
        case ".xls":
        case ".xlsx":
            return "cell";
        case ".ppt":
        case ".pptx":
            return "slide";
        default:
            return "word";
    }
}
```

**Testing**: Test with different file types (Word, Excel, PowerPoint)

---

### **Step 5: Add Dynamic Key Generation**

**Goal**: Generate proper document key for versioning

**Update OnlyOfficeManager**:
```csharp
public object GetConfig(int fileId, string baseUrl)
{
    var file = _repository.GetFileById(fileId);
    
    var config = new
    {
        document = new
        {
            fileType = GetFileExtension(file.OriginalName),
            key = GenerateDocumentKey(file), // Dynamic key
            title = file.OriginalName,
            url = $"{baseUrl}/api/onlyoffice/download/{fileId}",
            permissions = new { edit = true, download = true, print = true }
        },
        documentType = GetDocumentType(file.OriginalName),
        editorConfig = new { mode = "edit" }
    };

    var jwtSecret = "1Z8ezN1VlhBy95axTeD6yIi51PZGGmyk";
    var token = GenerateJwtToken(config, jwtSecret);

    return new { document = config.document, documentType = config.documentType, editorConfig = config.editorConfig, token = token };
}

private string GenerateDocumentKey(FileEntity file)
{
    // Generate unique key for OnlyOffice document versioning
    return $"file-{file.Id}-{DateTime.UtcNow:yyyyMMddHHmmssffff}";
}
```

**Testing**: Verify document opens and key changes between requests

---

### **Step 6: Add Dynamic Content Type**

**Goal**: Proper content-type detection for file downloads

**Update OnlyOfficeManager**:
```csharp
public FileDownloadResult GetFileForDownload(int fileId)
{
    var file = _repository.GetFileById(fileId);
    var fileBytes = File.ReadAllBytes(file.FilePath);
    
    return new FileDownloadResult
    {
        Content = fileBytes,
        ContentType = GetContentType(file.OriginalName), // Dynamic content type
        FileName = file.OriginalName
    };
}

private string GetContentType(string fileName)
{
    var extension = Path.GetExtension(fileName).ToLowerInvariant();
    switch (extension)
    {
        case ".docx":
            return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
        case ".doc":
            return "application/msword";
        case ".xlsx":
            return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        case ".xls":
            return "application/vnd.ms-excel";
        case ".pptx":
            return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
        case ".ppt":
            return "application/vnd.ms-powerpoint";
        case ".pdf":
            return "application/pdf";
        default:
            return "application/octet-stream";
    }
}
```

**Testing**: Check proper MIME types in browser developer tools

---

### **Step 7: Move Configuration to Config Files**

**Goal**: Remove hardcoded values and use configuration files

**.NET Framework (web.config)**:
```xml
<appSettings>
  <add key="OnlyOffice.DocumentServerUrl" value="http://localhost:3131/" />
  <add key="OnlyOffice.JwtSecret" value="1Z8ezN1VlhBy95axTeD6yIi51PZGGmyk" />
</appSettings>
```

**Update OnlyOfficeManager Constructor**:
```csharp
public class OnlyOfficeManager
{
    private readonly IOnlyOfficeRepository _repository;
    private readonly string _jwtSecret;
    private readonly string _documentServerUrl;

    public OnlyOfficeManager(IOnlyOfficeRepository repository)
    {
        _repository = repository;
        _jwtSecret = ConfigurationManager.AppSettings["OnlyOffice.JwtSecret"]; // .NET Framework
        _documentServerUrl = ConfigurationManager.AppSettings["OnlyOffice.DocumentServerUrl"];
        
        if (string.IsNullOrEmpty(_jwtSecret))
            throw new InvalidOperationException("OnlyOffice.JwtSecret not configured");
    }

    public object GetConfig(int fileId, string baseUrl)
    {
        // ... existing code ...
        var token = GenerateJwtToken(config, _jwtSecret); // Use configuration
        // ... rest of method
    }
}
```

**Testing**: Verify configuration is loaded correctly and document still opens

---

## 🔮 Future Enhancements

Once the basic implementation is complete and working:

### **Security & Authentication**
- Add proper user context to OnlyOffice operations
- Implement file access permissions based on user roles
- Add audit logging for document access

### **Advanced Features**
- Document versioning and change tracking
- Real-time collaborative editing enhancements
- Custom OnlyOffice toolbar and features
- Document templates and auto-saving

### **Performance & Monitoring**
- Add caching for frequently accessed configurations
- Implement file streaming for large documents
- Add performance monitoring and logging
- Database query optimization

### **Enterprise Integration**
- WebDAV integration for external file access
- Integration with document management systems
- Advanced permission management
- SSO integration

---

## 🧪 Testing Strategy

### **After Each Step**
1. **Test in OnlyOffice Component**: Open document and verify it loads
2. **Check Browser Network Tab**: Verify API calls return expected data
3. **Check OnlyOffice Logs**: Look for JWT validation errors or file access issues
4. **Test Different File Types**: Ensure Word, Excel, PowerPoint files work

### **Common Issues & Solutions**

**JWT Token Issues**:
- Check OnlyOffice Document Server logs for "Invalid token" errors
- Verify JWT secret matches between backend and OnlyOffice configuration
- Test JWT token structure at jwt.io

**File Download Issues**:
- Check file paths are correct and files exist on disk
- Verify content-type headers are properly set
- Test download endpoint directly in browser

**Authentication Issues**:
- Ensure OnlyOffice download endpoint doesn't require authentication
- Check that repository can access files without user context
- Verify session management doesn't interfere

---

## 🚨 Risk Mitigation

### **Rollback Strategy**
- Keep original frontend JWT generation until backend is fully working
- Test each step thoroughly before proceeding to next
- Maintain backup of working configuration

### **Deployment Notes**
- Test in staging environment first
- Update configuration files with production values
- Monitor OnlyOffice Document Server logs during initial deployment
- Have rollback plan ready for production deployment

---

This roadmap follows your divide-and-conquer approach with practical, testable steps that build incrementally toward a complete solution compatible with .NET Framework 4.5.6 and Angular 17.x.x.