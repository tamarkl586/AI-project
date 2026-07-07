import { Injectable, signal, computed } from '@angular/core';
import { environment } from '../../../environments/environment';
import { Observable, tap, map, catchError, of } from 'rxjs';
import { LoginModel } from '../../models/user/LoginModel';
import { RegisterModel } from '../../models/user/RegisterModel';
import { HttpClient } from '@angular/common/http';

interface LoginResponse {
  token: string;
  message: string;
}

@Injectable({
  providedIn: 'root',
})
export class AuthService {
  private baseUrl = `${environment.apiUrl}/auth`;

  private _isLoggedIn = signal(this.hasToken());
  isLoggedIn = this._isLoggedIn.asReadonly();

  currentRole = computed(() => this._isLoggedIn() ? this.getRole() : null);
  currentUserName = computed(() => this._isLoggedIn() ? this.getUserName() : null);
  isManager = computed(() => this.currentRole() === 'manager');

  constructor(private http: HttpClient) {}

  login(dto: LoginModel): Observable<LoginResponse> {
    return this.http.post(`${this.baseUrl}/login`, dto, { responseType: 'text' }).pipe(
      tap(rawToken => {
        let cleanToken = rawToken;
        try {
          const parsed = JSON.parse(rawToken);
          if (typeof parsed === 'string') {
            cleanToken = parsed;
          } else if (parsed?.token) {
            cleanToken = parsed.token;
          }
        } catch {
          // Already a raw token string — use as-is
        }
        cleanToken = cleanToken.replace(/^"|"$/g, '').trim();
        localStorage.setItem('token', cleanToken);
        this._isLoggedIn.set(true);
      }),
      map(token => ({ token: token, message: 'Logged in successfully' }))
    );
  }
  // -------------------------

  register(dto: RegisterModel): Observable<any> {
    return this.http.post(`${this.baseUrl}/register`, dto);
  }

  checkEmailExists(email: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.baseUrl}/check-email`, {
      params: { email }
    }).pipe(
      catchError(() => of(false))
    );
  }

  checkNameExists(name: string): Observable<boolean> {
    return this.http.get<boolean>(`${this.baseUrl}/check-name`, {
      params: { name }
    }).pipe(
      catchError(() => of(false))
    );
  }

  logout(): void {
    localStorage.removeItem('token');
    this._isLoggedIn.set(false);
  }

  getToken(): string | null {
    return localStorage.getItem('token');
  }

  private hasToken(): boolean {
    return !!localStorage.getItem('token');
  }

  getRole(): string | null {
    const payload = this.decodeToken();
    return payload?.['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] ?? null;
  }

  getUserName(): string | null {
    const payload = this.decodeToken();
    return payload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name'] ?? null;
  }

  getUserId(): number | null {
    const payload = this.decodeToken();
    const id = payload?.['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier'];
    return id ? +id : null;
  }

  private decodeToken(): any {
    const token = this.getToken();
    if (!token) return null;
    try {
      const payload = token.split('.')[1];
      const binaryString = atob(payload);
      const bytes = Uint8Array.from(binaryString, c => c.charCodeAt(0));
      return JSON.parse(new TextDecoder().decode(bytes));
    } catch {
      return null;
    }
  }
}


