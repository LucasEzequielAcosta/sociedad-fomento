import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, of, switchMap, tap, catchError } from 'rxjs';
import { AdminIdentity } from './api-models';

/** Manages the administrator session against the cookie-based API. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);

  readonly currentAdmin = signal<AdminIdentity | null>(null);
  private sessionLoaded = false;

  /** Loads the antiforgery cookie and creates an admin session. */
  login(email: string, password: string): Observable<AdminIdentity> {
    return this.http.get('/api/auth/antiforgery', { responseType: 'text' }).pipe(
      switchMap(() => this.http.post('/api/auth/login', { email, password }, { responseType: 'text' })),
      switchMap(() => this.http.get<AdminIdentity>('/api/auth/me')),
      tap((identity) => {
        this.currentAdmin.set(identity);
        this.sessionLoaded = true;
      })
    );
  }

  /** Ends the current admin session. */
  logout(): Observable<void> {
    return this.http.post('/api/auth/logout', {}, { responseType: 'text' }).pipe(
      map(() => undefined),
      tap(() => this.currentAdmin.set(null))
    );
  }

  /** Resolves the admin session once per app lifetime. */
  ensureSession(): Observable<AdminIdentity | null> {
    if (this.sessionLoaded) {
      return of(this.currentAdmin());
    }
    return this.http.get<AdminIdentity>('/api/auth/me').pipe(
      tap((identity) => {
        this.currentAdmin.set(identity);
        this.sessionLoaded = true;
      }),
      catchError(() => {
        this.sessionLoaded = true;
        return of(null);
      })
    );
  }
}
