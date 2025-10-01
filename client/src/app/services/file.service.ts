import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';
import { IConfig } from '@onlyoffice/document-editor-angular';
import { FileItem, UploadResponse, IOnlyOfficeConfig, FilesResponse } from '../models';
import { environment } from '../app.config';

@Injectable({
  providedIn: 'root'
})
export class FileService {
  private readonly apiUrl = environment.apiUrl;

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

  getFiles(): Observable<FilesResponse> {
    return this.http.get<FilesResponse>(
      `${this.apiUrl}/files`,
      this.getHttpOptionsWithHeaders()
    );
  }

  downloadFile(fileId: string): Observable<Blob> {
    // Add timestamp to bust browser cache
    const timestamp = new Date().getTime();
    return this.http.get(
      `${this.apiUrl}/files/${fileId}/download?t=${timestamp}`,
      {
        responseType: 'blob',
        withCredentials: true
      }
    );
  }

  deleteFile(fileId: string): Observable<{message: string}> {
    return this.http.delete<{message: string}>(
      `${this.apiUrl}/files/${fileId}`,
      this.getHttpOptionsWithHeaders()
    );
  }

  saveFile(tempId: string): Observable<UploadResponse> {
    return this.http.post<UploadResponse>(
      `${this.apiUrl}/files/${tempId}/save`,
      {},
      this.getHttpOptionsWithHeaders()
    );
  }

  getOnlyOfficeConfig(fileId: string): Observable<IOnlyOfficeConfig> {
    return this.http.get<IOnlyOfficeConfig>(
      `${this.apiUrl}/onlyoffice/config/${fileId}`,
      this.getHttpOptionsWithHeaders()
    );
  }


  // Helper method to trigger file download in browser
  downloadFileAsBlob(fileId: string, fileName: string): void {
    // Only allow downloads of saved files (Guid format: 36 chars with dashes)
    if (fileId.length !== 36 || !fileId.includes('-')) {
      console.error('Cannot download temporary files');
      return;
    }

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