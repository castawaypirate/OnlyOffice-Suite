import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DocumentEditorModule } from '@onlyoffice/document-editor-angular';

@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    DocumentEditorModule
  ],
  exports: [
    DocumentEditorModule
  ]
})
export class OnlyOfficeModule { }