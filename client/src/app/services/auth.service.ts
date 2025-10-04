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
  userId?: string;
  username?: string;
}

export interface AuthStatus {
  isAuthenticated: boolean;
  userId?: string;
  username?: string;
}

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiUrl = 'http://localhost:5142/api';
  private readonly AUTH_STORAGE_KEY = 'onlyoffice_auth';
  private currentUserSubject = new BehaviorSubject<AuthStatus>({ isAuthenticated: false });
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    // Load auth state from localStorage immediately (synchronous, no delay)
    this.loadAuthFromStorage();
    // Then verify with server in background
    this.checkAuthStatus();
  }

  private loadAuthFromStorage(): void {
    try {
      const storedAuth = localStorage.getItem(this.AUTH_STORAGE_KEY);
      if (storedAuth) {
        const authStatus: AuthStatus = JSON.parse(storedAuth);
        this.currentUserSubject.next(authStatus);
      }
    } catch (error) {
      console.error('Failed to load auth from storage:', error);
      localStorage.removeItem(this.AUTH_STORAGE_KEY);
    }
  }

  private saveAuthToStorage(authStatus: AuthStatus): void {
    try {
      localStorage.setItem(this.AUTH_STORAGE_KEY, JSON.stringify(authStatus));
    } catch (error) {
      console.error('Failed to save auth to storage:', error);
    }
  }

  private clearAuthFromStorage(): void {
    localStorage.removeItem(this.AUTH_STORAGE_KEY);
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
            const authStatus: AuthStatus = {
              isAuthenticated: true,
              userId: response.userId,
              username: response.username
            };
            this.currentUserSubject.next(authStatus);
            this.saveAuthToStorage(authStatus);
          }
        })
      );
  }

  logout(): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/logout`, {}, this.getHttpOptions())
      .pipe(
        tap(() => {
          this.currentUserSubject.next({ isAuthenticated: false });
          this.clearAuthFromStorage();
        })
      );
  }

  checkAuthStatus(): void {
    this.http.get<AuthStatus>(`${this.apiUrl}/auth/status`, this.getHttpOptions())
      .subscribe({
        next: (status) => {
          this.currentUserSubject.next(status);
          // Sync localStorage with server response
          if (status.isAuthenticated) {
            this.saveAuthToStorage(status);
          } else {
            // Session expired on server, clear local storage
            this.clearAuthFromStorage();
          }
        },
        error: () => {
          // Server error or network issue, clear everything
          this.currentUserSubject.next({ isAuthenticated: false });
          this.clearAuthFromStorage();
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