import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IConfig } from '@onlyoffice/document-editor-angular';
import { FileService } from '../services/file.service';
import { IOnlyOfficeConfig } from '../models';

@Component({
  selector: 'app-document-editor-page',
  templateUrl: './document-editor-page.component.html',
  styleUrls: ['./document-editor-page.component.css'],
  standalone: false
})
export class DocumentEditorPageComponent implements OnInit {
  fileId!: string;
  fileName = '';
  documentServerUrl = '';
  config: IConfig | null = null;
  editorKey = '';
  docEditor: any = null; // Reference to OnlyOffice editor instance

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fileService: FileService
  ) {}

  ngOnInit() {
    this.fileId = this.route.snapshot.paramMap.get('fileId') || '';
    this.loadFileData();
  }

  private loadFileData() {
    try {
      const fileIdNum = parseInt(this.fileId, 10);
      
      this.fileService.getOnlyOfficeConfig(fileIdNum).subscribe({
        next: (backendConfig: IOnlyOfficeConfig) => {
          // Backend returns nested config with onlyOfficeServerUrl
          this.config = backendConfig.config;
          this.documentServerUrl = backendConfig.onlyOfficeServerUrl;

          this.fileName = backendConfig.config.document?.title || 'Document';

          // Generate unique editor key to force recreation
          this.editorKey = `editor-${this.fileId}-${this.config.document?.key || 'unknown'}-${Date.now()}`;
        },
        error: (error) => {
          console.error('Failed to load OnlyOffice config:', error);
          this.fileName = 'Error loading document';
        }
      });
    } catch (error) {
      console.error('Invalid file ID:', error);
      this.fileName = 'Invalid document ID';
    }
  }



  goBack() {
    this.router.navigate(['/files']);
  }

  saveAndClose() {
    if (this.docEditor && typeof this.docEditor.forceSave === 'function') {
      console.log('🔧 Forcing document save before closing...');
      // Force save triggers Status 6 callback immediately
      this.docEditor.forceSave();

      // Wait a moment for the callback to complete, then navigate
      setTimeout(() => {
        this.router.navigate(['/files']);
      }, 2000); // 2 seconds should be enough for force save callback
    } else {
      // Fallback if forceSave not available
      this.router.navigate(['/files']);
    }
  }

  onDocumentReady(event: any) {
    console.log('🔧 Document editor is ready for file:', this.fileId);
    // Store reference to editor instance for force save
    this.docEditor = event;
  }

  onDocumentStateChange(event: any) {
    console.log('Document state changed:', event);
  }

  onError(event: any) {
    console.log('OnlyOffice error:', event);
  }
}