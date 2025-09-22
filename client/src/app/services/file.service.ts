import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

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

export interface OnlyOfficeConfig {
  document: {
    fileType: string;
    key: string;
    title: string;
    url: string;
    permissions: {
      edit: boolean;
      download: boolean;
      print: boolean;
    };
  };
  documentType: string;
  editorConfig: {
    mode: string;
  };
  token: string;
}

@Injectable({
  providedIn: 'root'
})
export class FileService {
  private readonly apiUrl = 'http://localhost:5142/api';

  constructor(private http: HttpClient) {}

  private getHttpOptions() {
    return {
      withCredentials: true
    };
  }

  private getHttpOptionsWithHeaders() {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      }),
      withCredentials: true
    };
  }

  uploadFile(file: File): Observable<UploadResponse> {
    const formData = new FormData();
    formData.append('file', file);

    return this.http.post<UploadResponse>(
      `${this.apiUrl}/files/upload`, 
      formData, 
      this.getHttpOptions()
    );
  }

  getFiles(): Observable<FileItem[]> {
    return this.http.get<FileItem[]>(
      `${this.apiUrl}/files`, 
      this.getHttpOptionsWithHeaders()
    );
  }

  downloadFile(fileId: number): Observable<Blob> {
    return this.http.get(
      `${this.apiUrl}/files/${fileId}/download`,
      {
        responseType: 'blob',
        withCredentials: true
      }
    );
  }

  deleteFile(fileId: number): Observable<{message: string}> {
    return this.http.delete<{message: string}>(
      `${this.apiUrl}/files/${fileId}`,
      this.getHttpOptionsWithHeaders()
    );
  }

  getOnlyOfficeConfig(fileId: number): Observable<OnlyOfficeConfig> {
    return this.http.get<OnlyOfficeConfig>(
      `${this.apiUrl}/onlyoffice/config/${fileId}`,
      this.getHttpOptionsWithHeaders()
    );
  }

  // Helper method to trigger file download in browser
  downloadFileAsBlob(fileId: number, fileName: string): void {
    this.downloadFile(fileId).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = fileName;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
        window.URL.revokeObjectURL(url);
      },
      error: (error) => {
        console.error('Download failed:', error);
      }
    });
  }
}