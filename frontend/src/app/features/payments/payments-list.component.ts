import { Component, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { provideNativeDateAdapter } from '@angular/material/core';
import { PaymentListItem, PaymentMethod } from '../../core/api-models';

/** Payments list with member/date/method filters, cancellation and CSV export. */
@Component({
  selector: 'app-payments-list',
  providers: [provideNativeDateAdapter()],
  imports: [
    CurrencyPipe,
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatDatepickerModule,
  ],
  templateUrl: './payments-list.component.html',
})
export class PaymentsListComponent {
  private readonly http = inject(HttpClient);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly columns = ['date', 'member', 'periods', 'amount', 'method', 'status', 'actions'];
  protected readonly payments = signal<PaymentListItem[]>([]);
  protected readonly cancellingId = signal<number | null>(null);
  protected readonly filters = this.formBuilder.nonNullable.group({
    memberId: [null as number | null],
    from: [null as Date | null],
    to: [null as Date | null],
    method: ['' as '' | PaymentMethod],
  });

  constructor() {
    this.search();
  }

  protected search(): void {
    let params = new HttpParams();
    const { memberId, from, to, method } = this.filters.getRawValue();
    if (memberId) {
      params = params.set('memberId', memberId);
    }
    if (from) {
      params = params.set('from', from.toISOString().slice(0, 10));
    }
    if (to) {
      params = params.set('to', to.toISOString().slice(0, 10));
    }
    if (method) {
      params = params.set('method', method);
    }
    this.http.get<PaymentListItem[]>('/api/payments', { params }).subscribe((items) => this.payments.set(items));
  }

  protected cancel(paymentId: number): void {
    this.http.post(`/api/payments/${paymentId}/cancel`, {}).subscribe(() => {
      this.cancellingId.set(null);
      this.search();
    });
  }

  protected exportUrl(): string {
    const { memberId, from, to, method } = this.filters.getRawValue();
    const query = new URLSearchParams();
    if (memberId) {
      query.set('memberId', String(memberId));
    }
    if (from) {
      query.set('from', from.toISOString().slice(0, 10));
    }
    if (to) {
      query.set('to', to.toISOString().slice(0, 10));
    }
    if (method) {
      query.set('method', method);
    }
    const suffix = query.size ? `?${query}` : '';
    return `/api/payments/export${suffix}`;
  }

  protected methodLabel(method: string): string {
    const labels: Record<string, string> = {
      Cash: 'Efectivo',
      BankTransfer: 'Transferencia',
      DirectDebit: 'Débito automático',
    };
    return labels[method] ?? method;
  }
}
