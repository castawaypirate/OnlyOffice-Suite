# OnlyOffice-Angular Suite - Project Specification

## Project Overview
A proof-of-concept web application that integrates OnlyOffice Document Server for collaborative document editing. The application provides secure file management and session-based authentication.

**Note**: This is a test project to evaluate OnlyOffice integration patterns for later implementation in a legacy application using .NET Framework 4.5.6 and Angular 17+, which follows the Manager-Repository design pattern.

## Core Features
1. **Authentication System**: Simple username/password login with server-side sessions
2. **File Management**: Upload, list, and download files with user-specific access control
3. **OnlyOffice Integration**: Real-time document editing through OnlyOffice Document Server
4. **Security**: Token-based file access for OnlyOffice with expiration

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

### File Security
- **Access Control**: Users can only access their own files through web interface
- **OnlyOffice Tokens**: Time-limited tokens for document server access

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
4. **OnlyOffice Edit**: `/editor/{fileId}` with token authentication

## OnlyOffice Integration

### Document Server Configuration

**OnlyOffice Document Server Setup**:
- **This POC Environment**: Self-hosted OnlyOffice Document Server on Linux (native installation, no Docker) - Port 3131
- **Target Legacy Environment**: Self-hosted OnlyOffice Document Server on Windows (native installation, no Docker) - Port 80
- **Server URL**: `http://localhost:3131/` for POC, `http://localhost/` for legacy (configurable in appsettings.json)
- **Installation Type**: Both environments use native OnlyOffice installations without containerization
- **Authentication**: JWT tokens generated server-side using Newtonsoft.Json
- **File Access**: Via direct file serving
- **Save Callback**: Documents saved back to file system

### Document Editor Flow
1. User clicks "Edit" button on file
2. Backend generates time-limited token
3. Frontend loads OnlyOffice editor with token
4. OnlyOffice Document Server accesses file via token
5. Changes saved back to file system

## API Endpoints

### Authentication
- `POST /api/auth/login` - User login with username/password
- `POST /api/auth/logout` - User logout and session cleanup
- `GET /api/auth/status` - Check current authentication status

### File Management
- `GET /api/files` - List user's files with metadata
- `POST /api/files/upload` - Upload new file (multipart/form-data)
- `GET /api/files/{id}/download` - Download file by ID
- `DELETE /api/files/{id}` - Delete file by ID

### OnlyOffice Integration (Planned)
- `GET /api/files/{id}/editor-config` - Get OnlyOffice configuration
- `POST /api/files/{id}/callback` - OnlyOffice save callback

## Current Working Features

### Authentication System
- **Web Login**: Session-based authentication with cookies
- **User Isolation**: Each user can only access their own files
- **Test Accounts**: admin/admin123 (userId=1), user1/password (userId=2)

### File Management
- **Web Upload**: Drag & drop or click to upload files
- **Web Download**: Direct download with original filenames
- **File Operations**: Upload, download, delete, list with metadata

## Development Setup

### Prerequisites
- Node.js 18+ (for Angular frontend)
- .NET 9 SDK
- OnlyOffice Document Server running on port 3131 (optional for now)

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
- ✅ .NET Core Web API backend setup
- ✅ SQLite database with Entity Framework Core
- ✅ User and FileEntity models with relationships
- ✅ Session-based authentication (login/logout/status endpoints)
- ✅ Database seeding with test users (admin/admin123, user1/password)
- ✅ CORS configuration for Angular frontend
- ✅ **File upload/download endpoints** (POST /api/files/upload, GET /api/files/{id}/download)
- ✅ **File management APIs** (GET /api/files, DELETE /api/files/{id})
- ✅ **Frontend integration with real backend APIs** (FileService, real file operations)
- ❌ OnlyOffice Document Server integration endpoints
- ❌ Frontend OnlyOffice editor connected to backend files

## Future Enhancements (Post-POC)
- Password hashing and proper security
- User management interface
- File versioning system
- Collaborative editing features
- WebDAV integration for external file access
- File sharing between users
- Audit logging
- File type validation and virus scanning

## Technical Debt / Future Refactoring
- **Configuration Management**: Create dedicated `OnlyOfficeSettings` class instead of reading config directly in managers
- **JWT Service**: Extract JWT generation into separate service class for better separation of concerns
- **Authentication Strategy**: Implement proper authentication for OnlyOffice controller endpoints

---

## Changelog

### 2025-09-17 - Initial Specification
- **Created**: Complete project specification document
- **Defined**: Technical architecture with .NET backend and SQLite database
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

### 2025-09-17 - File Management System Complete
- **Backend APIs**: Complete file upload/download/delete system with FileService
- **File Storage**: User-specific directories with unique filename handling
- **File Metadata**: Size calculation, content-type detection, upload tracking
- **Frontend Integration**: Angular FileService with real API communication
- **User Interface**: File upload, download, delete with progress indicators
- **Cross-Compatibility**: Files uploaded via web appear in database immediately
- **Error Handling**: Comprehensive error states and user feedback

### Future Entries
*Changelog entries will be added here as development progresses*