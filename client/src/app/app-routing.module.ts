import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { LoginComponent } from './login/login.component';
import { FileListComponent } from './file-list/file-list.component';
import { DocumentEditorPageComponent } from './document-editor-page/document-editor-page.component';
import { AuthGuard } from './guards/auth.guard';

const routes: Routes = [
  { path: '', redirectTo: '/login', pathMatch: 'full' },
  { path: 'login', component: LoginComponent },
  { path: 'files', component: FileListComponent, canActivate: [AuthGuard] },
  { path: 'editor/:fileId', component: DocumentEditorPageComponent, canActivate: [AuthGuard] }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule { }