import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { MemberDetail, MemberStatement, MemberSummary, PagedResult } from '../../core/api-models';

export interface MemberSearchCriteria {
  dni?: string;
  name?: string;
  lastName?: string;
  page?: number;
  pageSize?: number;
}

export interface MemberPersonalData {
  firstName: string;
  lastName: string;
  address: string;
  birthDate: string;
  phone: string;
  email?: string | null;
}

/** Accesses the administrative members API. */
@Injectable({ providedIn: 'root' })
export class MemberService {
  private readonly http = inject(HttpClient);

  search(criteria: MemberSearchCriteria): Observable<PagedResult<MemberSummary>> {
    let params = new HttpParams()
      .set('page', criteria.page ?? 1)
      .set('pageSize', criteria.pageSize ?? 20);
    if (criteria.dni) {
      params = params.set('dni', criteria.dni);
    }
    if (criteria.name) {
      params = params.set('name', criteria.name);
    }
    if (criteria.lastName) {
      params = params.set('lastName', criteria.lastName);
    }
    return this.http.get<PagedResult<MemberSummary>>('/api/members', { params });
  }

  getById(id: number): Observable<MemberDetail> {
    return this.http.get<MemberDetail>(`/api/members/${id}`);
  }

  getByDni(dni: string): Observable<MemberDetail> {
    return this.http.get<MemberDetail>(`/api/members/by-dni/${encodeURIComponent(dni)}`);
  }

  getStatement(id: number): Observable<MemberStatement> {
    return this.http.get<MemberStatement>(`/api/member-statements/${id}`);
  }

  create(dni: string, data: MemberPersonalData): Observable<MemberDetail> {
    return this.http.post<MemberDetail>('/api/members', { dni, ...data });
  }

  updatePersonalData(id: number, data: MemberPersonalData): Observable<MemberDetail> {
    return this.http.put<MemberDetail>(`/api/members/${id}/personal-data`, data);
  }

  deactivate(id: number): Observable<void> {
    return this.http.post<void>(`/api/members/${id}/deactivate`, {});
  }

  reactivate(id: number): Observable<MemberDetail> {
    return this.http.post<MemberDetail>(`/api/members/${id}/reactivate`, {});
  }

  exportCsv(criteria: MemberSearchCriteria): string {
    const query = new URLSearchParams();
    if (criteria.dni) {
      query.set('dni', criteria.dni);
    }
    if (criteria.name) {
      query.set('name', criteria.name);
    }
    if (criteria.lastName) {
      query.set('lastName', criteria.lastName);
    }
    const suffix = query.size ? `?${query}` : '';
    return `/api/members/export${suffix}`;
  }
}
