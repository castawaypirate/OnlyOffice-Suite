import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService, AuthStatus } from '../services/auth.service';
import { FileService, FileItem } from '../services/file.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-file-list',
  templateUrl: './file-list.component.html',
  styleUrls: ['./file-list.component.css'],
  standalone: false
})
export class FileListComponent implements OnInit {
  files: FileItem[] = [];
  currentUser$: Observable<AuthStatus>;
  isLoading = false;
  errorMessage = '';
  selectedFile: File | null = null;

  constructor(
    private router: Router,
    private authService: AuthService,
    private fileService: FileService
  ) {
    this.currentUser$ = this.authService.currentUser$;
  }

  ngOnInit() {
    // Check if user is authenticated, redirect to login if not
    if (!this.authService.isAuthenticated) {
      this.router.navigate(['/login']);
      return;
    }
    
    this.loadFiles();
  }

  loadFiles() {
    this.isLoading = true;
    this.errorMessage = '';
    
    this.fileService.getFiles().subscribe({
      next: (files) => {
        this.files = files;
        this.isLoading = false;
      },
      error: (error) => {
        console.error('Failed to load files:', error);
        this.errorMessage = 'Failed to load files. Please try again.';
        this.isLoading = false;
      }
    });
  }

  onFileSelected(event: any) {
    const file = event.target.files[0];
    if (file) {
      this.selectedFile = file;
      this.uploadFile();
    }
  }

  uploadFile() {
    if (!this.selectedFile) return;

    this.isLoading = true;
    this.errorMessage = '';

    this.fileService.uploadFile(this.selectedFile).subscribe({
      next: () => {
        // File uploaded successfully
        this.selectedFile = null;
        this.loadFiles(); // Reload the file list
      },
      error: (error) => {
        console.error('File upload failed:', error);
        this.errorMessage = error.error?.message || 'File upload failed. Please try again.';
        this.isLoading = false;
      }
    });
  }

  downloadFile(file: FileItem) {
    this.fileService.downloadFileAsBlob(file.id, file.name);
  }

  deleteFile(file: FileItem) {
    if (!confirm(`Are you sure you want to delete "${file.name}"?`)) {
      return;
    }

    this.fileService.deleteFile(file.id).subscribe({
      next: () => {
        // File deleted successfully
        this.loadFiles(); // Reload the file list
      },
      error: (error) => {
        console.error('File deletion failed:', error);
        this.errorMessage = error.error?.message || 'File deletion failed. Please try again.';
      }
    });
  }

  openDocument(fileId: number) {
    this.router.navigate(['/editor', fileId]);
  }

  onLogout() {
    this.authService.logout().subscribe({
      next: () => {
        // Logout successful
        this.router.navigate(['/login']);
      },
      error: (error) => {
        console.error('Logout failed:', error);
        // Even if logout fails, redirect to login
        this.router.navigate(['/login']);
      }
    });
  }
}