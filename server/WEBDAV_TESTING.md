# WebDAV Testing Guide

## WebDAV Endpoints

The WebDAV server is available at: `http://localhost:5142/webdav/{userId}/`

### Authentication
- **Method**: HTTP Basic Authentication
- **Credentials**: Same as web login (admin/admin123 or user1/password)
- **User ID**: 1 for admin, 2 for user1

## Testing with Command Line

### 1. Test OPTIONS (Server Capabilities)
```bash
curl -X OPTIONS http://localhost:5142/webdav/1/ -v
```

### 2. Test PROPFIND (List Files)
```bash
curl -X PROPFIND http://localhost:5142/webdav/1/ \
  -u admin:admin123 \
  -H "Content-Type: application/xml" \
  -v
```

### 3. Upload File with PUT
```bash
curl -X PUT http://localhost:5142/webdav/1/test.txt \
  -u admin:admin123 \
  -d "Hello WebDAV!" \
  -H "Content-Type: text/plain" \
  -v
```

### 4. Download File with GET
```bash
curl -X GET http://localhost:5142/webdav/1/test.txt \
  -u admin:admin123 \
  -v
```

### 5. Delete File
```bash
curl -X DELETE http://localhost:5142/webdav/1/test.txt \
  -u admin:admin123 \
  -v
```

## Testing with File Managers

### Windows Explorer
1. Open File Explorer
2. Right-click "This PC" → "Map network drive"
3. Choose a drive letter
4. Folder: `http://localhost:5142/webdav/1/`
5. Check "Connect using different credentials"
6. Enter: admin / admin123

### macOS Finder
1. Open Finder
2. Press Cmd+K (Go → Connect to Server)
3. Server Address: `http://localhost:5142/webdav/1/`
4. Click Connect
5. Enter credentials: admin / admin123

### Linux (Various File Managers)
Most Linux file managers support WebDAV:
- Nautilus: "Other Locations" → "Connect to Server"
- Dolphin: "Network" → "Add Network Folder"
- URL: `webdav://localhost:5142/webdav/1/`

## Expected Behavior

1. **PROPFIND**: Should return XML listing all user's files
2. **GET**: Should download files uploaded via web interface
3. **PUT**: Should upload files that appear in web interface
4. **DELETE**: Should remove files from both WebDAV and web interface
5. **Cross-compatibility**: Files uploaded via web should be accessible via WebDAV and vice versa

## User Directories

- User ID 1 (admin): `/webdav/1/`
- User ID 2 (user1): `/webdav/2/`

Each user can only access their own directory.