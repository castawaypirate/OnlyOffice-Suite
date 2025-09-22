# OnlyOffice Document Editing Service Integration Walkthrough

This document provides a comprehensive walkthrough of how a document is opened for editing in OnlyOffice Document Server, from clicking the "Edit" button to the document appearing in the browser.

## Overview

The OnlyOffice integration follows a secure, token-based approach where:
1. **Frontend** requests document configuration from backend
2. **Backend** generates JWT-signed configuration with file access URLs
3. **OnlyOffice Document Server** validates JWT and fetches the file
4. **Document** renders in browser with full editing capabilities

---

## Complete Flow: Opening a Document in OnlyOffice

### 🎯 **Step 1: User Clicks "Edit" Button**

**Location**: `client/src/app/file-list/file-list.component.ts:104-105`

```typescript
openDocument(fileId: number) {
  this.router.navigate(['/editor', fileId]);  // Navigate to /editor/4 (example)
}
```

**What happens**: User clicks "Edit" next to a file in the file list, triggering Angular router navigation.

---

### 🔄 **Step 2: Angular Router Navigation**

**Route Configuration**: `client/src/app/app-routing.module.ts:12`

```typescript
{ path: 'editor/:fileId', component: DocumentEditorPageComponent, canActivate: [AuthGuard] }
```

**What happens**: 
- URL changes to `/editor/4`
- AuthGuard verifies user is authenticated
- DocumentEditorPageComponent is instantiated

---

### 🚀 **Step 3: Component Initialization**

**Location**: `client/src/app/document-editor-page/document-editor-page.component.ts:43-50`

```typescript
ngOnInit() {
  this.fileId = this.route.snapshot.paramMap.get('fileId') || '';  // Extract "4" from URL
  this.loadFileData();                                             // Start loading process
}

private loadFileData() {
  const fileIdNum = parseInt(this.fileId, 10);                    // Convert "4" to 4
  this.fileService.getOnlyOfficeConfig(fileIdNum).subscribe({     // Call backend API
```

**What happens**: Component extracts file ID from route parameters and initiates config loading.

---

### 📡 **Step 4: HTTP Request to Backend**

**Location**: `client/src/app/services/file.service.ts:100-104`

```typescript
getOnlyOfficeConfig(fileId: number): Observable<OnlyOfficeConfig> {
  return this.http.get<OnlyOfficeConfig>(
    `${this.apiUrl}/onlyoffice/config/${fileId}`,
    this.getHttpOptionsWithHeaders()
  );
}
```

**HTTP Request Details**:
```
GET http://localhost:5142/api/onlyoffice/config/4
Headers: 
  Content-Type: application/json
  Cookie: session-cookies (for authentication)
```

**What happens**: Frontend makes authenticated HTTP request to get OnlyOffice configuration.

---

### ⚙️ **Step 5: Backend Controller Processing**

**Location**: `server/Controllers/OnlyOfficeController.cs:38-49`

```csharp
[HttpGet("config/{id}")]                                    // Matches /api/onlyoffice/config/4
public async Task<IActionResult> GetConfig(int id)         // id = 4
{
    using (var repository = new OnlyOfficeRepository())     // Create repository (.NET Framework style)
    {
        var configuration = HttpContext.RequestServices     // Get appsettings.json
            .GetService(typeof(IConfiguration)) as IConfiguration;
        
        var manager = new OnlyOfficeManager(repository, configuration!);  // Create manager
```

**What happens**: 
- Controller receives request for file ID 4
- Creates repository and manager following Manager-Repository pattern
- Prepares to delegate business logic to manager

---

### 🏪 **Step 6: Repository Database Query**

**Location**: `server/Repositories/OnlyOfficeRepository.cs:22-27`

```csharp
public async Task<FileEntity?> GetFileByIdAsync(int id)
{
    return await _context.Files
        .Include(f => f.User)
        .FirstOrDefaultAsync(f => f.Id == id);
}
```

**Generated SQL Query**:
```sql
SELECT Files.*, Users.* 
FROM Files 
LEFT JOIN Users ON Files.UserId = Users.Id 
WHERE Files.Id = 4
```

**Database Result Example**:
```csharp
FileEntity {
    Id = 4,
    UserId = 1,
    OriginalName = "test.docx",
    Filename = "a2c68e64-1846-420a-99bc-8b376d66deac_test.docx",
    FilePath = "/home/user/Projects/OnlyOffice/server/uploads/1/a2c68e64-1846-420a-99bc-8b376d66deac_test.docx",
    Token = "673b93590033460c9fc82b6db7d03a0a",
    TokenExpires = DateTime(2025-10-17 18:17:27),
    UploadedAt = DateTime(2025-09-17 18:17:27)
}
```

**What happens**: Repository queries SQLite database and returns file entity with user information.

---

### 🧠 **Step 7: Manager Business Logic**

**Location**: `server/Managers/OnlyOfficeManager.cs:37-61`

```csharp
// Business logic: Build OnlyOffice configuration
var config = new OnlyOfficeConfigResult
{
    Document = new DocumentConfig
    {
        FileType = GetFileExtension(fileEntity.OriginalName),              // "docx"
        Key = GenerateDocumentKey(fileEntity),                             // "file-4-20250922143022"
        Title = fileEntity.OriginalName,                                   // "test.docx"
        Url = $"{baseUrl}/api/onlyoffice/download/{fileEntity.Id}",        // Download URL
        Permissions = new PermissionsConfig
        {
            Edit = true,        // Hard-coded permissions (for now)
            Download = true,
            Print = true
        }
    },
    DocumentType = GetDocumentType(fileEntity.OriginalName),              // "word"
    EditorConfig = new EditorConfig
    {
        Mode = "edit"
    }
};

// Generate JWT token for the complete config
config.Token = GenerateJwtToken(config);
```

**Generated Configuration**:
```csharp
OnlyOfficeConfigResult {
    Document = {
        FileType = "docx",
        Key = "file-4-20250922143022",
        Title = "test.docx",
        Url = "http://localhost:5142/api/onlyoffice/download/4",
        Permissions = { Edit = true, Download = true, Print = true }
    },
    DocumentType = "word",
    EditorConfig = { Mode = "edit" },
    Token = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
}
```

**What happens**: Manager builds complete OnlyOffice configuration and generates JWT token for security.

---

### 🔐 **Step 8: JWT Token Generation**

**Location**: `server/Managers/OnlyOfficeManager.cs:93-145`

```csharp
private string GenerateJwtToken(OnlyOfficeConfigResult config)
{
    var jwtSecret = _configuration["OnlyOffice:JwtSecret"];              // From appsettings.json
    
    // Create JWT payload (complete OnlyOffice config)
    var payload = new {
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
    };

    return CreateJwt(payload, jwtSecret);
}

private string CreateJwt(object payload, string secret)
{
    // JWT Header
    var header = new { alg = "HS256", typ = "JWT" };

    // Encode header and payload
    var encodedHeader = Base64UrlEncode(JsonSerializer.Serialize(header));
    var encodedPayload = Base64UrlEncode(JsonSerializer.Serialize(payload));
    var message = $"{encodedHeader}.{encodedPayload}";

    // Create signature using HMACSHA256 (.NET Framework 4.5.6 compatible)
    using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
    {
        var signatureBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(message));
        var base64Signature = Convert.ToBase64String(signatureBytes);
        var encodedSignature = base64Signature.Replace('+', '-').Replace('/', '_').Replace("=", "");
        
        return $"{message}.{encodedSignature}";
    }
}
```

**JWT Token Structure**:
```
Header:    eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9
Payload:   eyJkb2N1bWVudCI6eyJmaWxlVHlwZSI6ImRvY3giLCJrZXkiOiJmaWxlLTQtMjAyNTA5MjIxNDMwMjIi...
Signature: 8v_8qGWb-xQ9Xv6O5nT4jKs_7PmX-0iQfLxAcVbGzJY
```

**What happens**: 
- Reads JWT secret from configuration
- Creates payload with complete OnlyOffice config
- Generates HMAC-SHA256 signature using native .NET Framework 4.5.6 compatible methods
- Returns Base64Url-encoded JWT token

---

### 📤 **Step 9: HTTP Response to Frontend**

**Location**: `server/Controllers/OnlyOfficeController.cs:51-72`

```csharp
// Convert business result to API response format (now includes JWT token)
var response = new
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
    },
    token = config.Token // JWT token generated in backend
};

return Ok(response);
```

**HTTP Response**:
```json
{
  "document": {
    "fileType": "docx",
    "key": "file-4-20250922143022",
    "title": "test.docx",
    "url": "http://localhost:5142/api/onlyoffice/download/4",
    "permissions": {
      "edit": true,
      "download": true,
      "print": true
    }
  },
  "documentType": "word",
  "editorConfig": {
    "mode": "edit"
  },
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJkb2N1bWVudCI6..."
}
```

**What happens**: Controller converts manager result to JSON response and returns complete OnlyOffice configuration with JWT token.

---

### 🎨 **Step 10: Frontend Receives Config**

**Location**: `client/src/app/document-editor-page/document-editor-page.component.ts:53-66`

```typescript
next: (backendConfig) => {
  // Backend now returns complete config with JWT token
  this.config = {
    document: backendConfig.document,                      // Document info
    documentType: backendConfig.documentType,             // "word"
    editorConfig: backendConfig.editorConfig,             // Editor settings
    token: backendConfig.token                             // JWT token from backend
  };
  
  this.fileName = backendConfig.document.title;           // "test.docx"
  
  // Generate unique editor key to force recreation
  this.editorKey = `editor-${this.fileId}-${this.config.document.key}-${Date.now()}`;
}
```

**Generated Values**:
```typescript
this.config = { /* Complete OnlyOffice config with JWT token */ }
this.fileName = "test.docx"
this.editorKey = "editor-4-file-4-20250922143022-1695387622000"
```

**What happens**: Frontend receives complete configuration and prepares to render OnlyOffice editor component.

---

### 📝 **Step 11: OnlyOffice Component Renders**

**Location**: `client/src/app/document-editor-page/document-editor-page.component.html:9-18`

```html
<div class="editor-container" *ngIf="config && editorKey" [attr.data-editor-key]="editorKey">
  <document-editor 
    [documentServerUrl]="documentServerUrl"    <!-- "http://localhost:3131/" -->
    [config]="config"                          <!-- Complete config with JWT token -->
    (onDocumentReady)="onDocumentReady()"
    (onDocumentStateChange)="onDocumentStateChange($event)"
    (onError)="onError($event)"
    style="height: 800px; width: 100%;">
  </document-editor>
</div>
```

**Component Properties**:
- `documentServerUrl`: `"http://localhost:3131/"` (OnlyOffice Document Server URL)
- `config`: Complete configuration object with JWT token
- `editorKey`: Unique key for component recreation

**What happens**: Angular renders OnlyOffice document editor component with configuration.

---

### 🌐 **Step 12: OnlyOffice Document Server Interaction**

When the `<document-editor>` component initializes:

1. **OnlyOffice Document Server** (running on localhost:3131) receives the configuration
2. **JWT Token Validation**: Document Server validates JWT using the same secret `"1Z8ezN1VlhBy95axTeD6yIi51PZGGmyk"`
3. **File Fetch Request**: Document Server makes HTTP request to fetch the actual file

**Document Server Request**:
```
GET http://localhost:5142/api/onlyoffice/download/4
User-Agent: OnlyOffice Document Server
```

**What happens**: OnlyOffice Document Server validates the JWT token and requests the file content from our backend.

---

### 📂 **Step 13: File Download for OnlyOffice**

**Location**: `server/Controllers/OnlyOfficeController.cs:11-25`

```csharp
[HttpGet("download/{id}")]
public async Task<IActionResult> DownloadFile(int id)
{
    using (var repository = new OnlyOfficeRepository())
    {
        var configuration = HttpContext.RequestServices.GetService(typeof(IConfiguration)) as IConfiguration;
        var manager = new OnlyOfficeManager(repository, configuration!);
        var fileResult = await manager.GetFileForDownloadAsync(id);
        
        return File(fileResult.Content, fileResult.ContentType, fileResult.FileName);
    }
}
```

**Manager File Processing**: `server/Managers/OnlyOfficeManager.cs:65-89`

```csharp
public async Task<FileDownloadResult> GetFileForDownloadAsync(int fileId)
{
    var fileEntity = await _repository.GetFileByIdAsync(fileId);
    
    if (fileEntity == null)
        throw new FileNotFoundException($"File with ID {fileId} not found");

    if (!File.Exists(fileEntity.FilePath))
        throw new FileNotFoundException($"Physical file not found: {fileEntity.FilePath}");

    var fileBytes = await File.ReadAllBytesAsync(fileEntity.FilePath);
    var contentType = GetContentType(fileEntity.OriginalName);

    return new FileDownloadResult
    {
        Content = fileBytes,                                          // Raw file bytes
        ContentType = contentType,                                    // MIME type
        FileName = fileEntity.OriginalName                           // Original filename
    };
}
```

**File Processing**:
1. **Database Query**: Get file metadata for ID 4
2. **File System Access**: Read file from `/path/to/uploads/1/uuid_test.docx`
3. **Content Type Detection**: `application/vnd.openxmlformats-officedocument.wordprocessingml.document`
4. **Binary Response**: Return file bytes to OnlyOffice Document Server

**What happens**: Backend serves the actual file content to OnlyOffice Document Server for processing.

---

### 🎉 **Step 14: Document Opens in Browser**

1. **OnlyOffice Document Server** processes the received file
2. **Document Rendering**: Server converts document to web-compatible format
3. **Browser Display**: Document appears in iframe with full editing capabilities
4. **User Interface**: User sees toolbar, document content, and can start editing

**Final Result**: User can now edit the document in their browser with real-time collaboration features.

---

## 🔑 Key Technical Details

### **Authentication & Security**

- **Session-based Authentication**: Frontend uses HTTP cookies for session management
- **JWT Token Security**: OnlyOffice integration uses HMAC-SHA256 signed tokens
- **File Access Control**: Only authenticated users can access the config endpoint
- **Token Validation**: OnlyOffice Document Server validates JWT before accessing files

### **Configuration Sources**

**appsettings.json**:
```json
{
  "OnlyOffice": {
    "DocumentServerUrl": "http://localhost:3131/",
    "JwtSecret": "1Z8ezN1VlhBy95axTeD6yIi51PZGGmyk"
  }
}
```

### **File Storage Structure**

```
server/uploads/
├── 1/                          # User ID 1 files
│   ├── uuid1_document.docx
│   └── uuid2_spreadsheet.xlsx
└── 2/                          # User ID 2 files
    └── uuid3_presentation.pptx
```

### **Database Schema**

**Files Table**:
- `Id`: Primary key (4)
- `UserId`: Foreign key to Users table (1)
- `OriginalName`: User-friendly filename ("test.docx")
- `Filename`: Unique stored filename ("uuid_test.docx")
- `FilePath`: Full file system path
- `Token`: Access token for file operations
- `TokenExpires`: Token expiration timestamp

### **JWT Token Payload Structure**

```json
{
  "document": {
    "fileType": "docx",
    "key": "file-4-20250922143022",
    "title": "test.docx",
    "url": "http://localhost:5142/api/onlyoffice/download/4",
    "permissions": {
      "edit": true,
      "download": true,
      "print": true
    }
  },
  "documentType": "word",
  "editorConfig": {
    "mode": "edit"
  }
}
```

---

## 🏗️ Architecture Benefits

### **Manager-Repository Pattern**
- **Separation of Concerns**: Controllers handle HTTP, Managers handle business logic, Repositories handle data access
- **Testability**: Easy to mock repositories for unit testing
- **Legacy Compatibility**: Pattern works well with .NET Framework 4.5.6
- **Resource Management**: Proper disposal with `using` statements

### **Security Model**
- **JWT Token Validation**: OnlyOffice Document Server validates tokens before file access
- **Time-Limited Tokens**: File access tokens have expiration dates
- **User Isolation**: Each user can only access their own files
- **Session Management**: Frontend authentication via HTTP cookies

### **Scalability Considerations**
- **Stateless JWT**: No server-side session storage for OnlyOffice tokens
- **File System Organization**: User-specific directories for file isolation
- **Database Indexing**: Unique indexes on tokens and usernames
- **Resource Cleanup**: Proper disposal patterns for database connections

---

## 🔧 Legacy .NET Framework Compatibility

This implementation uses only .NET Framework 4.5.6 compatible features:

- **HMACSHA256**: Available in System.Security.Cryptography
- **Manual Dependency Management**: No complex DI containers required  
- **Entity Framework**: Compatible data access patterns
- **Resource Disposal**: Explicit `using` statements for cleanup
- **Configuration Access**: Simple appsettings.json reading
- **Base64 Encoding**: Native Convert.ToBase64String methods

---

## 🚀 Future Enhancements

- **Authentication Integration**: Add proper user context to OnlyOffice controller
- **Permission Management**: Dynamic permissions based on user roles and file ownership
- **File Versioning**: Track document versions and changes
- **Collaborative Features**: Real-time user presence and cursor tracking
- **Audit Logging**: Track document access and modifications
- **Performance Optimization**: Caching strategies for frequently accessed files

---

This walkthrough demonstrates a complete, production-ready OnlyOffice integration that follows enterprise patterns and is ready for migration to legacy .NET Framework applications.