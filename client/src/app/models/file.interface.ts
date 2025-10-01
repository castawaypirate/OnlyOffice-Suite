export interface FileItem {
  id: string;
  name: string;
  filename: string | null;
  size: string;
  uploadDate: Date;
  token: string | null;
  isTemporary: boolean;
}

export interface UploadResponse {
  id: string;
  originalName: string;
  filename?: string;
  size: string;
  uploadedAt: Date;
  isTemporary: boolean;
  message: string;
}

export interface FilesResponse {
  files: FileItem[];
  features: {
    onlyOfficeEnabled: boolean;
  };
}