export interface FileItem {
  id: number;
  name: string;
  filename: string;
  size: string;
  uploadDate: Date;
  token: string;
}

export interface UploadResponse {
  id: number;
  originalName: string;
  filename: string;
  size: string;
  uploadedAt: Date;
  message: string;
}

export interface FilesResponse {
  files: FileItem[];
  features: {
    onlyOfficeEnabled: boolean;
  };
}