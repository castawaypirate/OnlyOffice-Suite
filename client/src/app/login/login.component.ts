import { Component } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css'],
  standalone: false
})
export class LoginComponent {
  username = '';
  password = '';

  constructor(private router: Router) {}

  onLogin() {
    // Mock authentication - accept any credentials
    if (this.username && this.password) {
      // In real app, set authentication cookie here
      console.log('Mock login successful for:', this.username);
      this.router.navigate(['/files']);
    }
  }
}