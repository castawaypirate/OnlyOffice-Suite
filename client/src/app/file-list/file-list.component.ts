import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService, AuthStatus } from '../services/auth.service';
import { Observable } from 'rxjs';

interface FileItem {
  id: string;
  name: string;
  uploadDate: Date;
  size: string;
}

@Component({
  selector: 'app-file-list',
  templateUrl: './file-list.component.html',
  styleUrls: ['./file-list.component.css'],
  standalone: false
})
export class FileListComponent implements OnInit {
  files: FileItem[] = [
    {
      id: 'sample-1',
      name: 'Sample Document.docx',
      uploadDate: new Date(),
      size: '240 bytes'
    }
  ];
  currentUser$: Observable<AuthStatus>;

  constructor(
    private router: Router,
    private authService: AuthService
  ) {
    this.currentUser$ = this.authService.currentUser$;
  }

  ngOnInit() {
    // Check if user is authenticated, redirect to login if not
    if (!this.authService.isAuthenticated) {
      this.router.navigate(['/login']);
    }
  }

  openDocument(fileId: string) {
    this.router.navigate(['/editor', fileId]);
  }

  onLogout() {
    this.authService.logout().subscribe({
      next: () => {
        console.log('Logout successful');
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