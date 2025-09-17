import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { tap } from 'rxjs/operators';

export interface LoginRequest {
  username: string;
  password: string;
}

export interface AuthResponse {
  message: string;
  userId?: number;
  username?: string;
}

export interface AuthStatus {
  isAuthenticated: boolean;
  userId?: number;
  username?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = 'http://localhost:5142/api';
  private currentUserSubject = new BehaviorSubject<AuthStatus>({ isAuthenticated: false });
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    this.checkAuthStatus();
  }

  private getHttpOptions() {
    return {
      headers: new HttpHeaders({
        'Content-Type': 'application/json'
      }),
      withCredentials: true
    };
  }

  login(credentials: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, credentials, this.getHttpOptions())
      .pipe(
        tap(response => {
          if (response.userId && response.username) {
            this.currentUserSubject.next({
              isAuthenticated: true,
              userId: response.userId,
              username: response.username
            });
          }
        })
      );
  }

  logout(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/logout`, {}, this.getHttpOptions())
      .pipe(
        tap(() => {
          this.currentUserSubject.next({ isAuthenticated: false });
        })
      );
  }

  checkAuthStatus(): void {
    this.http.get<AuthStatus>(`${this.apiUrl}/auth/status`, this.getHttpOptions())
      .subscribe({
        next: (status) => {
          this.currentUserSubject.next(status);
        },
        error: () => {
          this.currentUserSubject.next({ isAuthenticated: false });
        }
      });
  }

  get currentUser(): AuthStatus {
    return this.currentUserSubject.value;
  }

  get isAuthenticated(): boolean {
    return this.currentUser.isAuthenticated;
  }
}