# OnlyOffice-Angular Suite - Project Specification

## Project Overview
A proof-of-concept web application that integrates OnlyOffice Document Server for collaborative document editing. The application provides secure file management with WebDAV access and session-based authentication.

## Core Features
1. **Authentication System**: Simple username/password login with server-side sessions
2. **File Management**: Upload, list, and download files with user-specific access control
3. **OnlyOffice Integration**: Real-time document editing through OnlyOffice Document Server
4. **WebDAV Access**: File access through WebDAV protocol for external clients
5. **Security**: Token-based file access for OnlyOffice with expiration

## Technical Architecture

### Frontend (Angular 20.3.0)
- **Location**: `/client/` directory
- **Framework**: Angular with TypeScript 5.9.2
- **Build System**: Angular CLI with Vite
- **Styling**: Component-scoped CSS
- **Testing**: Jasmine + Karma

### Backend (.NET Core)
- **Location**: `/server/` directory
- **Framework**: ASP.NET Core
- **Database**: SQLite with Entity Framework Core
- **Authentication**: Session-based with HTTP cookies
- **WebDAV**: Custom WebDAV server implementation

### Database Schema (SQLite with Entity Framework Core)

#### User Model
```csharp
public class User
{
    public int Id { get; set; }
    [Required, MaxLength(100)]
    public string Username { get; set; } = string.Empty;
    [Required, MaxLength(255)]
    public string Password { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();
}
```

#### FileEntity Model
```csharp
public class FileEntity
{
    public int Id { get; set; }
    public int UserId { get; set; }
    [Required, MaxLength(255)]
    public string Filename { get; set; } = string.Empty;
    [Required, MaxLength(255)]
    public string OriginalName { get; set; } = string.Empty;
    [Required, MaxLength(500)]
    public string FilePath { get; set; } = string.Empty;
    [Required, MaxLength(100)]
    public string Token { get; set; } = string.Empty;
    public DateTime TokenExpires { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public User User { get; set; } = null!;
}
```

#### Database Configuration
- **Database Creation**: Automatic via `context.Database.EnsureCreated()`
- **Relationships**: Configured in `AppDbContext.OnModelCreating()`
- **Constraints**: Username unique index, Token unique index, cascade delete
- **Default Values**: CreatedAt and UploadedAt set to current UTC time

## Authentication & Security

### Web Authentication
- **Method**: Server-side sessions with HTTP cookies
- **Session Storage**: In-memory distributed cache
- **Session Timeout**: 30 minutes idle timeout
- **User Creation**: Database seeding with test users (admin/admin123, user1/password)
- **Password Security**: Plain text storage (POC only - not production ready)

### WebDAV Authentication
- **Method**: HTTP Basic Authentication
- **Credentials**: Same username/password as web login
- **Integration**: Authenticates against same Users table

### File Security
- **Access Control**: Users can only access their own files through web interface
- **OnlyOffice Tokens**: Time-limited tokens for document server access
- **WebDAV Structure**: User-specific directories (`/webdav/{userId}/`)

## File Management

### File Storage
- **Location**: `/server/uploads/{userId}/`
- **Organization**: User-specific directories
- **Naming**: Preserve original filenames with collision handling
- **Types**: All file types accepted (testing focuses on .docx)

### File Operations
1. **Upload**: POST to `/api/files/upload`
2. **List**: GET `/api/files` (returns user's files only)
3. **Download**: GET `/api/files/{id}/download`
4. **WebDAV Access**: `/webdav/{userId}/{filename}`
5. **OnlyOffice Edit**: `/editor/{fileId}` with token authentication

## OnlyOffice Integration

### Document Server Configuration
- **URL**: `http://localhost:3131/` (configurable)
- **Authentication**: JWT tokens generated server-side
- **File Access**: Via WebDAV or direct file serving
- **Save Callback**: Documents saved back via WebDAV

### Document Editor Flow
1. User clicks "Edit" button on file
2. Backend generates time-limited token
3. Frontend loads OnlyOffice editor with token
4. OnlyOffice Document Server accesses file via token
5. Changes saved back to WebDAV location

## WebDAV Implementation

### Endpoint Structure
- **Root**: `/webdav/`
- **User Directories**: `/webdav/{userId}/`
- **File Access**: `/webdav/{userId}/{filename}`

### Supported Operations
- **PROPFIND**: Directory listing
- **GET**: File download
- **PUT**: File upload/update
- **DELETE**: File removal
- **MKCOL**: Directory creation

### Authentication
- **Method**: HTTP Basic Authentication
- **Validation**: Against Users table
- **Access Control**: Users can only access their own directory

## API Endpoints

### Authentication
- `POST /api/auth/login` - User login
- `POST /api/auth/logout` - User logout
- `GET /api/auth/status` - Check authentication status

### File Management
- `GET /api/files` - List user's files
- `POST /api/files/upload` - Upload new file
- `GET /api/files/{id}` - Get file metadata
- `GET /api/files/{id}/download` - Download file
- `DELETE /api/files/{id}` - Delete file

### OnlyOffice Integration
- `GET /api/files/{id}/editor-config` - Get OnlyOffice configuration
- `POST /api/files/{id}/callback` - OnlyOffice save callback

## Development Setup

### Prerequisites
- Node.js 18+ (for Angular frontend)
- .NET 8 SDK
- OnlyOffice Document Server running on port 3131

### Project Structure
```
OnlyOffice-Suite/
├── client/                 # Angular frontend
│   ├── src/app/
│   ├── package.json
│   └── angular.json
├── server/                 # .NET backend
│   ├── Controllers/       # API controllers (Auth, Files, etc.)
│   ├── Models/           # Entity models (User, FileEntity)
│   ├── Services/         # Business logic services
│   ├── Data/             # DbContext and database configuration
│   ├── uploads/          # File storage directory
│   ├── Program.cs        # Application entry point
│   └── OnlyOfficeServer.csproj
├── PROJECT_SPECIFICATION.md
└── README.md
```

### Current Implementation Status
- ✅ Angular frontend with OnlyOffice integration
- ✅ Mock authentication and file listing (frontend)
- ✅ OnlyOffice editor component
- ✅ .NET Core Web API backend setup
- ✅ SQLite database with Entity Framework Core
- ✅ User and FileEntity models with relationships
- ✅ Session-based authentication (login/logout/status endpoints)
- ✅ Database seeding with test users
- ✅ CORS configuration for Angular frontend
- ❌ File upload/download endpoints
- ❌ WebDAV server implementation
- ❌ OnlyOffice integration endpoints
- ❌ Frontend integration with real backend APIs

## Future Enhancements (Post-POC)
- Password hashing and proper security
- User management interface
- File versioning system
- Collaborative editing features
- Advanced WebDAV features (locking, versioning)
- File sharing between users
- Audit logging
- File type validation and virus scanning

---

## Changelog

### 2025-09-17 - Initial Specification
- **Created**: Complete project specification document
- **Defined**: Technical architecture with .NET backend and SQLite database
- **Planned**: WebDAV integration with user-specific directories
- **Established**: Security model with session-based auth and file tokens
- **Documented**: Database schema and API endpoints
- **Identified**: Current implementation status and gaps

### 2025-09-17 - Backend Foundation Complete
- **Implemented**: .NET Core Web API project with Entity Framework Core
- **Created**: User and FileEntity models with proper relationships and constraints
- **Configured**: SQLite database with automatic creation and seeding
- **Built**: Session-based authentication system with login/logout/status endpoints
- **Added**: Database seeding service with test users (admin/admin123, user1/password)
- **Setup**: CORS configuration for Angular frontend integration
- **Updated**: Project specification to reflect Entity Framework approach instead of raw SQL

### Future Entries
*Changelog entries will be added here as development progresses*