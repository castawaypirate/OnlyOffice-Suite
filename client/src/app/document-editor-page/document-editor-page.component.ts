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
  hasUncommittedChanges = false; // Track if document has uncommitted changes
  lastForceSaveCompleted = true; // Track if last force-save callback completed (starts true = freshly loaded)

  // Modal states
  showUncommittedModal = false;
  showSavingModal = false;
  savingModalMessage = 'Please wait while your document is being saved...';
  showSaveResultModal = false;
  saveResultTitle = '';
  saveResultMessage = '';
  saveResultSuccess = false;
  saveTimeout: any = null;

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

          // Handle auto-save completion
          if (data.source === 'auto-save') {
            if (data.success) {
              console.log('[COMPONENT] Auto force-save completed successfully');
              this.lastForceSaveCompleted = true;

              // If saving modal is open, close it and show success
              if (this.showSavingModal) {
                this.showSavingModal = false;
                this.showSaveResultModal = true;
                this.saveResultSuccess = true;
                this.saveResultTitle = 'Save Successful';
                this.saveResultMessage = 'Your document has been saved successfully.';

                // Clear timeout
                if (this.saveTimeout) {
                  clearTimeout(this.saveTimeout);
                }
              }
            } else {
              console.error('[COMPONENT] Auto force-save failed:', data.message);
              this.lastForceSaveCompleted = true; // Allow navigation attempt even on failure

              // If saving modal is open, show error
              if (this.showSavingModal) {
                this.showSavingModal = false;
                this.showSaveResultModal = true;
                this.saveResultSuccess = false;
                this.saveResultTitle = 'Save Failed';
                this.saveResultMessage = data.message || 'The document could not be saved. Please try again.';

                // Clear timeout
                if (this.saveTimeout) {
                  clearTimeout(this.saveTimeout);
                }
              }
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
    console.log('🔙 Back to Files clicked');
    console.log('State: hasUncommittedChanges =', this.hasUncommittedChanges, ', lastForceSaveCompleted =', this.lastForceSaveCompleted);

    // Check if there are uncommitted changes
    if (this.hasUncommittedChanges) {
      console.log('⚠️ Uncommitted changes detected, showing modal');
      this.showUncommittedModal = true;
      return;
    }

    // Check if force-save is in progress
    if (!this.lastForceSaveCompleted) {
      console.log('⏳ Force-save in progress, showing saving modal');
      this.showSavingModal = true;
      this.savingModalMessage = 'Please wait while your document is being saved...';

      // Set timeout for 15 seconds
      this.saveTimeout = setTimeout(() => {
        console.error('❌ Save timeout: No SignalR confirmation received');
        this.showSavingModal = false;
        this.showSaveResultModal = true;
        this.saveResultSuccess = false;
        this.saveResultTitle = 'Save Timeout';
        this.saveResultMessage = 'The save operation is taking longer than expected. Please try again or contact support.';
      }, 15000);

      return;
    }

    // All clear - navigate
    console.log('✅ All clear, navigating to files');
    this.router.navigate(['/files']);
  }

  onDocumentReady(event: any) {
    console.log('🔧 Document editor is ready for file:', this.fileId);

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
      console.log('✅ All changes committed (saved internally), triggering force-save');
      this.hasUncommittedChanges = false;

      // Reset flag to false when force-save is initiated (Option B)
      this.lastForceSaveCompleted = false;

      // Auto force-save to ensure physical file is always up-to-date
      const documentKey = this.config?.document?.key;
      if (documentKey) {
        console.log('💾 Triggering auto force-save with key:', documentKey);
        this.fileService.forceSaveDocument(this.fileId, documentKey, 'auto-save').subscribe({
          next: (result) => {
            if (result.error === 0) {
              console.log('✅ Auto force-save command sent successfully');
            } else if (result.error === 4) {
              console.log('ℹ️ Auto force-save: No changes to save (already saved)');
              // Error 4 means already saved, set flag back to true
              this.lastForceSaveCompleted = true;
            } else {
              console.warn('⚠️ Auto force-save command failed:', result.message);
              // On error, reset flag to true so user can try to navigate
              this.lastForceSaveCompleted = true;
            }
          },
          error: (error) => {
            console.error('❌ Auto force-save failed:', error);
            // On error, reset flag to true so user can try to navigate
            this.lastForceSaveCompleted = true;
          }
        });
      }
    }
  }

  onError(event: any) {
    console.log('OnlyOffice error:', event);
  }

  // Modal handlers
  closeUncommittedModal() {
    this.showUncommittedModal = false;
  }

  closeSaveResultModal() {
    this.showSaveResultModal = false;
    this.saveResultTitle = '';
    this.saveResultMessage = '';
    this.saveResultSuccess = false;
  }

  navigateAfterSaveSuccess() {
    this.closeSaveResultModal();
    this.router.navigate(['/files']);
  }
}