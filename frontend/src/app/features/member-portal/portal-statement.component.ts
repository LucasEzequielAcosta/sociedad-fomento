import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { PortalAuthService } from '../../core/portal-auth.service';
import { MemberStatement } from '../../core/api-models';

/** Member portal view of the member's own account statement. */
@Component({
  selector: 'app-portal-statement',
  imports: [CurrencyPipe, DatePipe, MatCardModule, MatTableModule, MatButtonModule],
  templateUrl: './portal-statement.component.html',
})
export class PortalStatementComponent {
  private readonly http = inject(HttpClient);
  protected readonly portalAuth = inject(PortalAuthService);
  private readonly router = inject(Router);

  protected readonly statement = signal<MemberStatement | null>(null);
  protected readonly columns = ['period', 'amount', 'status'];

  constructor() {
    this.http.get<MemberStatement>('/api/portal/statement').subscribe((statement) => this.statement.set(statement));
  }

  protected logout(): void {
    this.portalAuth.logout().subscribe(() => this.router.navigate(['/portal']));
  }

  protected statusLabel(status: string): string {
    const labels: Record<string, string> = {
      Pending: 'Pendiente',
      Paid: 'Pagada',
      Overdue: 'Vencida',
      Future: 'Futura',
    };
    return labels[status] ?? status;
  }
}
