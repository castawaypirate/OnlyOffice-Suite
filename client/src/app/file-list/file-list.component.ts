import { Component } from '@angular/core';
import { Router } from '@angular/router';

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
export class FileListComponent {
  files: FileItem[] = [
    {
      id: 'sample-1',
      name: 'Sample Document.docx',
      uploadDate: new Date(),
      size: '240 bytes'
    }
  ];

  constructor(private router: Router) {}

  openDocument(fileId: string) {
    this.router.navigate(['/editor', fileId]);
  }

  onLogout() {
    // In real app, clear authentication cookie here
    console.log('Logging out...');
    this.router.navigate(['/login']);
  }
}