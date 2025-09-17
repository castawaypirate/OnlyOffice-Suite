import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { OnlyOfficeModule } from '../onlyoffice.module';
import { DocumentEditorPageComponent } from '../document-editor-page/document-editor-page.component';

@NgModule({
  declarations: [
    DocumentEditorPageComponent
  ],
  imports: [
    CommonModule,
    RouterModule,
    OnlyOfficeModule
  ],
  exports: [
    DocumentEditorPageComponent
  ]
})
export class DocumentEditorModule { }