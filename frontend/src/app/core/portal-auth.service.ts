import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map, of, switchMap, tap, catchError } from 'rxjs';

export interface PortalIdentity {
  memberId: number;
  dni: string;
  firstName: string;
  lastName: string;
  status: 'Active' | 'Inactive';
}

/** Manages the member-portal session (DNI-only, separate cookie). */
@Injectable({ providedIn: 'root' })
export class PortalAuthService {
  private readonly http = inject(HttpClient);

  readonly currentMember = signal<PortalIdentity | null>(null);
  private sessionLoaded = false;

  /** Logs a member in using only their DNI. */
  login(dni: string): Observable<PortalIdentity> {
    return this.http.get('/api/auth/antiforgery', { responseType: 'text' }).pipe(
      switchMap(() => this.http.post('/api/portal/login', { dni }, { responseType: 'text' })),
      switchMap(() => this.http.get<PortalIdentity>('/api/portal/me')),
      tap((identity) => {
        this.currentMember.set(identity);
        this.sessionLoaded = true;
      })
    );
  }

  /** Ends the member-portal session. */
  logout(): Observable<void> {
    return this.http.post('/api/portal/logout', {}, { responseType: 'text' }).pipe(
      map(() => undefined),
      tap(() => this.currentMember.set(null))
    );
  }

  /** Resolves the member-portal session once per app lifetime. */
  ensureSession(): Observable<PortalIdentity | null> {
    if (this.sessionLoaded) {
      return of(this.currentMember());
    }
    return this.http.get<PortalIdentity>('/api/portal/me').pipe(
      tap((identity) => {
        this.currentMember.set(identity);
        this.sessionLoaded = true;
      }),
      catchError(() => {
        this.sessionLoaded = true;
        return of(null);
      })
    );
  }
}
