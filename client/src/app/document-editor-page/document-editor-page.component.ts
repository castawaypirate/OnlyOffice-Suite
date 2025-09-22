import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FileService, OnlyOfficeConfig } from '../services/file.service';

interface IConfig {
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
  token?: string;
}

@Component({
  selector: 'app-document-editor-page',
  templateUrl: './document-editor-page.component.html',
  styleUrls: ['./document-editor-page.component.css'],
  standalone: false
})
export class DocumentEditorPageComponent implements OnInit {
  fileId!: string;
  fileName = '';
  documentServerUrl = 'http://localhost:3131/';
  config: IConfig | null = null;
  editorKey = '';

  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private fileService: FileService
  ) {}

  async ngOnInit() {
    this.fileId = this.route.snapshot.paramMap.get('fileId') || '';
    await this.loadFileData();
  }

  private async loadFileData() {
    try {
      const fileIdNum = parseInt(this.fileId, 10);
      
      this.fileService.getOnlyOfficeConfig(fileIdNum).subscribe({
        next: async (backendConfig) => {
          // Convert backend config to IConfig format
          this.config = {
            document: backendConfig.document,
            documentType: backendConfig.documentType,
            editorConfig: backendConfig.editorConfig
          };
          
          this.fileName = backendConfig.document.title;
          
          // Generate JWT token from backend config
          this.config.token = await this.generateJWT(this.config);
          
          // Generate unique editor key to force recreation
          this.editorKey = `editor-${this.fileId}-${this.config.document.key}-${Date.now()}`;
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


  private async generateJWT(config: IConfig): Promise<string> {
    const secret = '1Z8ezN1VlhBy95axTeD6yIi51PZGGmyk';
    const token = await this.createJWT(config, secret);
    return token;
  }

  private async createJWT(payload: IConfig, secret: string): Promise<string> {
    const header = {
      alg: 'HS256',
      typ: 'JWT'
    };

    const base64UrlEncode = (str: string): string => {
      return btoa(str)
        .replace(/\+/g, '-')
        .replace(/\//g, '_')
        .replace(/=/g, '');
    };

    const encodedHeader = base64UrlEncode(JSON.stringify(header));
    const encodedPayload = base64UrlEncode(JSON.stringify(payload));
    const message = `${encodedHeader}.${encodedPayload}`;

    const encoder = new TextEncoder();
    const secretKey = await crypto.subtle.importKey(
      'raw',
      encoder.encode(secret),
      { name: 'HMAC', hash: 'SHA-256' },
      false,
      ['sign']
    );

    const signature = await crypto.subtle.sign(
      'HMAC',
      secretKey,
      encoder.encode(message)
    );

    const signatureArray = new Uint8Array(signature);
    const signatureString = String.fromCharCode(...signatureArray);
    const encodedSignature = base64UrlEncode(signatureString);

    return `${message}.${encodedSignature}`;
  }

  goBack() {
    this.router.navigate(['/files']);
  }

  onDocumentReady() {
    console.log('🔧 Document editor is ready for file:', this.fileId);
  }

  onDocumentStateChange(event: any) {
    console.log('Document state changed:', event);
  }

  onError(event: any) {
    console.log('OnlyOffice error:', event);
  }
}