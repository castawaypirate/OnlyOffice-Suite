import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { IConfig } from '@onlyoffice/document-editor-angular';
import { FileService } from '../services/file.service';
import { IOnlyOfficeConfig } from '../models';
import { SignalrService } from '../services/signalr.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-document-editor-page',
  templateUrl: './document-editor-page.component.html',
  styleUrls: ['./document-editor-page.component.css'],
  standalone: false
})
export class DocumentEditorPageComponent implements OnInit, OnDestroy {
  fileId!: string;
  fileName = '';
  documentServerUrl = '';
  config: IConfig | null = null;
  editorKey = '';
  docEditor: any = null; // Reference to OnlyOffice editor instance
  hasUncommittedChanges = true; // Track if document has uncommitted changes (starts true until first save)

  // Save & Close tracking
  isSaving = false;
  saveTimeout: any = null;
  pendingSaveAndClose = false;

  // Error modal
  showErrorModal = false;
  errorMessage = '';

  private signalrSubscriptions: Subscription[] = [];

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fileService: FileService,
    private signalrService: SignalrService
  ) {}

  ngOnInit() {
    this.fileId = this.route.snapshot.paramMap.get('fileId') || '';
    this.setupSignalR();
    this.loadFileData();
  }

  ngOnDestroy() {
    // Clear timeout if exists
    if (this.saveTimeout) {
      clearTimeout(this.saveTimeout);
    }

    // Unsubscribe from all SignalR events
    this.signalrSubscriptions.forEach(sub => sub.unsubscribe());

    // Leave the file room
    if (this.fileId) {
      this.signalrService.leaveFileRoom(this.fileId).catch(err => {
        console.error('[SIGNALR] Error leaving file room on destroy:', err);
      });
    }
  }

  private setupSignalR() {
    // Start SignalR connection
    this.signalrService.startConnection()
      .then(() => {
        // Join the room for this specific file
        return this.signalrService.joinFileRoom(this.fileId);
      })
      .then(() => {
        console.log(`[SIGNALR] Successfully joined room for file ${this.fileId}`);

        // Subscribe to callback events
        const callbackSub = this.signalrService.callbackReceived$.subscribe(data => {
          console.log('[COMPONENT] Callback received notification:', data);
        });

        const savedSub = this.signalrService.documentSaved$.subscribe(data => {
          console.log('[COMPONENT] Document saved notification:', data);
        });

        const forceSavedSub = this.signalrService.documentForceSaved$.subscribe(data => {
          console.log('[COMPONENT] Document force saved notification:', data);

          // Only navigate if this was OUR "save & close" operation
          if (this.pendingSaveAndClose && data.source === 'save-and-close') {
            console.log('[COMPONENT] Save & close confirmed, navigating to file list');

            if (data.success) {
              // Clear timeout and loading state
              if (this.saveTimeout) {
                clearTimeout(this.saveTimeout);
              }
              this.isSaving = false;
              this.pendingSaveAndClose = false;

              // Navigate with success state
              this.router.navigate(['/files'], {
                state: { showSaveSuccess: true, fileName: this.fileName }
              });
            } else {
              // Save failed
              this.isSaving = false;
              this.pendingSaveAndClose = false;
              if (this.saveTimeout) {
                clearTimeout(this.saveTimeout);
              }
              this.displayErrorModal('Save failed: ' + (data.message || 'Unknown error'));
            }
          }
        });

        this.signalrSubscriptions.push(callbackSub, savedSub, forceSavedSub);
      })
      .catch(err => {
        console.error('[SIGNALR] Failed to setup SignalR:', err);
      });
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
      this.displayErrorModal('Cannot save: Document key not available');
      return;
    }

    console.log('📤 Sending forceSave with key:', documentKey);

    // Set loading state
    this.isSaving = true;
    this.pendingSaveAndClose = true;

    // Call backend forcesave endpoint with source "save-and-close"
    this.fileService.forceSaveDocument(this.fileId, documentKey, 'save-and-close').subscribe({
      next: (result) => {
        console.log('✅ ForceSave command sent successfully:', result);

        if (result.error === 0) {
          console.log('✅ ForceSave command accepted, waiting for SignalR confirmation...');

          // Set timeout for 10 seconds
          this.saveTimeout = setTimeout(() => {
            console.error('❌ Save timeout: No SignalR confirmation received');
            this.isSaving = false;
            this.pendingSaveAndClose = false;
            this.displayErrorModal('Save timeout: The document may not have been saved. Please try again.');
          }, 10000);

        } else {
          console.error('❌ ForceSave command failed:', result.message);
          this.isSaving = false;
          this.pendingSaveAndClose = false;
          this.displayErrorModal('Failed to save: ' + (result.message || 'Unknown error'));
        }
      },
      error: (error) => {
        console.error('❌ Failed to send ForceSave command:', error);
        this.isSaving = false;
        this.pendingSaveAndClose = false;
        this.displayErrorModal('Error while saving: ' + (error.message || 'Network error'));
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

  displayErrorModal(message: string) {
    this.errorMessage = message;
    this.showErrorModal = true;
  }

  closeErrorModal() {
    this.showErrorModal = false;
    this.errorMessage = '';
  }
}