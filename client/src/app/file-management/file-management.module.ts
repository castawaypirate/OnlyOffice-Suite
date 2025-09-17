import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FileListComponent } from '../file-list/file-list.component';

@NgModule({
  declarations: [
    FileListComponent
  ],
  imports: [
    CommonModule,
    RouterModule
  ],
  exports: [
    FileListComponent
  ]
})
export class FileManagementModule { }