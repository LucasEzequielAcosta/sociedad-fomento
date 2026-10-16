import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';
import { Dashboard } from '../../core/api-models';

/** Administrative dashboard with member, obligation and economy totals. */
@Component({
  selector: 'app-dashboard',
  imports: [CurrencyPipe, DatePipe, MatCardModule, MatButtonModule, RouterLink],
  templateUrl: './dashboard.component.html',
})
export class DashboardComponent {
  private readonly http = inject(HttpClient);

  protected readonly dashboard = signal<Dashboard | null>(null);

  constructor() {
    this.http.get<Dashboard>('/api/dashboard').subscribe((data) => this.dashboard.set(data));
  }
}
