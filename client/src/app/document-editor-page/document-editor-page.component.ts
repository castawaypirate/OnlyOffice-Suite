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
  hasUncommittedChanges = true; // Track if document has uncommitted changes (starts true until first save)

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
      console.log('🔍 Loading file config for:', this.fileId);
      // fileId is now a Guid string, no need to parse
      this.fileService.getOnlyOfficeConfig(this.fileId).subscribe({
        next: (backendConfig: IOnlyOfficeConfig) => {
          console.log('✅ Config received from backend:', backendConfig);

          // Backend returns nested config with onlyOfficeServerUrl
          this.config = backendConfig.config;
          this.documentServerUrl = backendConfig.onlyOfficeServerUrl;

          // Add events to config
          this.config.events = {
            onDocumentReady: this.onDocumentReady.bind(this),
            onDocumentStateChange: this.onDocumentStateChange.bind(this),
            onError: this.onError.bind(this)
          };

          console.log('📄 Document Server URL:', this.documentServerUrl);
          console.log('📋 Config:', this.config);

          this.fileName = backendConfig.config.document?.title || 'Document';

          // Generate unique editor key to force recreation
          this.editorKey = `editor-${this.fileId}-${this.config.document?.key || 'unknown'}-${Date.now()}`;

          console.log('🔑 Editor key generated:', this.editorKey);
        },
        error: (error) => {
          console.error('❌ Failed to load OnlyOffice config:', error);
          this.fileName = 'Error loading document';
        }
      });
    } catch (error) {
      console.error('❌ Invalid file ID:', error);
      this.fileName = 'Invalid document ID';
    }
  }

  goBack() {
    this.router.navigate(['/files']);
  }

  saveAndClose() {
    console.log('💾 Save & Close button clicked');

    // Get the document key from the current config
    const documentKey = this.config?.document?.key;

    if (!documentKey) {
      console.error('❌ No document key available');
      this.router.navigate(['/files']);
      return;
    }

    console.log('📤 Sending forceSave with key:', documentKey);

    // Call backend forcesave endpoint with the current document key
    this.fileService.forceSaveDocument(this.fileId, documentKey).subscribe({
      next: (result) => {
        console.log('✅ ForceSave command sent successfully:', result);

        if (result.error === 0) {
          console.log('✅ Document saved successfully');
          // Wait a moment for the callback to complete, then navigate
          setTimeout(() => {
            this.router.navigate(['/files']);
          }, 2000); // 2 seconds for callback to process
        } else {
          console.error('❌ ForceSave command failed:', result.message);
          // Still navigate even if save failed
          this.router.navigate(['/files']);
        }
      },
      error: (error) => {
        console.error('❌ Failed to send ForceSave command:', error);
        // Navigate anyway to avoid getting stuck
        this.router.navigate(['/files']);
      }
    });
  }

  onDocumentReady(event: any) {
    console.log('🔧 Document editor is ready for file:', this.fileId);
    console.log('🔧 Editor instance:', event);
    console.log('🔧 forceSave available?', typeof event?.forceSave === 'function');

    // When document loads, assume no uncommitted changes (document is in saved state)
    this.hasUncommittedChanges = false;
  }

  onDocumentStateChange(event: any) {
    console.log('📝 Document state changed:', event);

    // OnlyOffice sends event.data = true when document has uncommitted changes
    // event.data = false means all changes have been committed (auto-saved internally)
    if (event && event.data === true) {
      console.log('⚠️ Document has uncommitted changes (editing in progress)');
      this.hasUncommittedChanges = true;
    } else if (event && event.data === false) {
      console.log('✅ All changes committed (saved internally, safe to close)');
      this.hasUncommittedChanges = false;
    }
  }

  onError(event: any) {
    console.log('OnlyOffice error:', event);
  }
}