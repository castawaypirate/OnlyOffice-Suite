import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';

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

  constructor(
    private route: ActivatedRoute,
    private router: Router
  ) {}

  async ngOnInit() {
    this.fileId = this.route.snapshot.paramMap.get('fileId') || '';
    
    // Mock file data - in real app, fetch from backend
    await this.loadFileData();
  }

  private async loadFileData() {
    // Mock delay to simulate API call
    await new Promise(resolve => setTimeout(resolve, 500));
    
    // Mock file data based on fileId
    switch (this.fileId) {
      case 'sample-1':
        this.fileName = 'Sample Document.docx';
        this.config = {
          document: {
            fileType: "docx",
            key: "test-document-key-" + Date.now(),
            title: "Sample Document.docx",
            url: "http://localhost:4200/sample.docx",
            permissions: {
              edit: true,
              download: true,
              print: true
            }
          },
          documentType: "word",
          editorConfig: {
            mode: "edit"
          }
        };
        
        // Generate JWT token
        this.config.token = await this.generateJWT(this.config);
        break;
      default:
        this.fileName = 'Unknown Document';
        // Handle unknown file
        break;
    }
  }

  private async generateJWT(config: IConfig): Promise<string> {
    const secret = '1Z8ezN1VlhBy95axTeD6yIi51PZGGmyk';
    return await this.createJWT(config, secret);
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
    console.log('Document editor is ready');
  }

  onDocumentStateChange(event: any) {
    console.log('Document state changed:', event);
  }

  onError(event: any) {
    console.log('OnlyOffice error:', event);
  }
}