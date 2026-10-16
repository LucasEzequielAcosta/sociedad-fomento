import { Component, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { provideNativeDateAdapter } from '@angular/material/core';
import { AccountingEntry, AccountingEntryType, AccountingSummary } from '../../core/api-models';

type SummaryPeriod = 'CurrentMonth' | 'PreviousMonth' | 'Year';

/** Accounting entries list, manual entry creation/cancellation, summary and CSV export. */
@Component({
  selector: 'app-accounting',
  providers: [provideNativeDateAdapter()],
  imports: [
    CurrencyPipe,
    DatePipe,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatDatepickerModule,
  ],
  templateUrl: './accounting.component.html',
})
export class AccountingComponent {
  private readonly http = inject(HttpClient);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly columns = ['date', 'type', 'source', 'concept', 'amount', 'status', 'actions'];
  protected readonly entries = signal<AccountingEntry[]>([]);
  protected readonly summary = signal<AccountingSummary | null>(null);
  protected readonly cancellingId = signal<number | null>(null);
  protected readonly busy = signal(false);
  protected readonly filters = this.formBuilder.nonNullable.group({
    from: [null as Date | null],
    to: [null as Date | null],
    type: ['' as '' | AccountingEntryType],
    concept: [''],
  });
  protected readonly summaryForm = this.formBuilder.nonNullable.group({
    period: ['CurrentMonth' as SummaryPeriod],
  });
  protected readonly entryForm = this.formBuilder.nonNullable.group({
    entryDate: [new Date(), Validators.required],
    type: ['Income' as AccountingEntryType, Validators.required],
    concept: ['', Validators.required],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
  });

  constructor() {
    this.search();
    this.loadSummary();
  }

  protected search(): void {
    this.http.get<AccountingEntry[]>('/api/accounting-entries', { params: this.filterParams() })
      .subscribe((entries) => this.entries.set(entries));
  }

  protected loadSummary(): void {
    const { period } = this.summaryForm.getRawValue();
    this.http.get<AccountingSummary>('/api/accounting-entries/summary', { params: { period } })
      .subscribe((summary) => this.summary.set(summary));
  }

  protected createEntry(): void {
    if (this.entryForm.invalid || this.busy()) {
      return;
    }
    this.busy.set(true);
    const { entryDate, type, concept, amount } = this.entryForm.getRawValue();
    this.http
      .post<AccountingEntry>('/api/accounting-entries', {
        entryDate: entryDate!.toISOString().slice(0, 10),
        type,
        concept,
        amount,
      })
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.entryForm.reset({ entryDate: new Date(), type: 'Income' });
          this.search();
          this.loadSummary();
        },
        error: () => this.busy.set(false),
      });
  }

  protected cancel(id: number): void {
    this.http.post(`/api/accounting-entries/${id}/cancel`, {}).subscribe(() => {
      this.cancellingId.set(null);
      this.search();
      this.loadSummary();
    });
  }

  protected exportUrl(): string {
    const params = this.filterParams();
    const suffix = params.keys().length ? `?${params.toString()}` : '';
    return `/api/accounting-entries/export${suffix}`;
  }

  protected typeLabel(type: string): string {
    return type === 'Income' ? 'Ingreso' : 'Egreso';
  }

  protected sourceLabel(source: string): string {
    return source === 'FeePayment' ? 'Pago de cuota' : 'Manual';
  }

  protected periodLabel(period: string): string {
    const labels: Record<string, string> = { CurrentMonth: 'Mes actual', PreviousMonth: 'Mes anterior', Year: 'Año' };
    return labels[period] ?? period;
  }

  private filterParams(): HttpParams {
    let params = new HttpParams();
    const { from, to, type, concept } = this.filters.getRawValue();
    if (from) {
      params = params.set('from', from.toISOString().slice(0, 10));
    }
    if (to) {
      params = params.set('to', to.toISOString().slice(0, 10));
    }
    if (type) {
      params = params.set('type', type);
    }
    if (concept) {
      params = params.set('concept', concept);
    }
    return params;
  }
}
