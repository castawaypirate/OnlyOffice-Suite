import { Component, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { AppRoutingModule } from './app-routing.module';
import { OnlyOfficeModule } from './onlyoffice.module';
import { AuthModule } from './auth/auth.module';
import { FileManagementModule } from './file-management/file-management.module';
import { DocumentEditorModule } from './document-editor/document-editor.module';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css',
  standalone: false
})
export class App {}

@NgModule({
  declarations: [App],
  imports: [
    BrowserModule, 
    AppRoutingModule, 
    OnlyOfficeModule,
    AuthModule,
    FileManagementModule,
    DocumentEditorModule
  ],
  providers: [],
  bootstrap: [App]
})
export class AppModule { }

