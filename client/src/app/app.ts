import { Component, NgModule } from '@angular/core';
import { BrowserModule } from '@angular/platform-browser';
import { provideHttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { AppRoutingModule } from './app-routing.module';
import { DocumentEditorModule } from '@onlyoffice/document-editor-angular';
import { DocumentEditorPageComponent } from './document-editor-page/document-editor-page.component';
import { LoginComponent } from './login/login.component';
import { FileListComponent } from './file-list/file-list.component';

@Component({
  selector: 'app-root',
  templateUrl: './app.html',
  styleUrl: './app.css',
  standalone: false
})
export class App {}

@NgModule({
  declarations: [
    App,
    DocumentEditorPageComponent,
    LoginComponent,
    FileListComponent
  ],
  imports: [
    BrowserModule,
    FormsModule,
    AppRoutingModule,
    DocumentEditorModule
  ],
  providers: [
    provideHttpClient()
  ],
  bootstrap: [App]
})
export class AppModule { }

