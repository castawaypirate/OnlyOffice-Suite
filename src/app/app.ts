import { Component, NgModule, OnInit } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { DocumentEditorModule } from '@onlyoffice/document-editor-angular';
import { SignJWT } from 'jose';

export interface IConfig {
  document: {
    fileType: string;
    key: string;
    title: string;
    url: string;
  };
  documentType: string;
  editorConfig: {
    callbackUrl: string;
  };
}

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css',
  standalone: false
})
export class App implements OnInit {
  title = 'OnlyOffice Angular Test';
  documentServerUrl = 'http://localhost:3131/';
  
  private async generateJWT(): Promise<string> {
    const secret = new TextEncoder().encode('1Z8ezN1VlhBy95axTeD6yIi51PZGGmyk');
    const payload = {
      document: {
        fileType: "docx",
        key: "test-document-key-" + Date.now(),
        title: "Sample Document.docx",
        url: "http://localhost:4200/assets/sample.docx",
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
    
    const jwt = await new SignJWT(payload)
      .setProtectedHeader({ alg: 'HS256' })
      .sign(secret);
    
    return jwt;
  }
  
  config: any = {
    document: {
      fileType: "docx",
      key: "test-document-key-" + Date.now(),
      title: "Sample Document.docx",
      url: "http://localhost:4200/assets/sample.docx",
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

  async ngOnInit() {
    this.config.token = await this.generateJWT();
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

@NgModule({
  declarations: [App],
  imports: [BrowserModule, DocumentEditorModule],
  providers: [],
  bootstrap: [App]
})
export class AppModule { }
