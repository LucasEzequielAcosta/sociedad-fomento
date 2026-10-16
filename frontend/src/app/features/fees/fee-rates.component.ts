import { Component, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { provideNativeDateAdapter } from '@angular/material/core';
import { FeeRate } from '../../core/api-models';

interface FeeGenerationResult {
  month: string;
  generatedCount: number;
}

/** Fee-rate history management and monthly obligation generation. */
@Component({
  selector: 'app-fee-rates',
  providers: [provideNativeDateAdapter()],
  imports: [
    CurrencyPipe,
    DatePipe,
    ReactiveFormsModule,
    MatCardModule,
    MatTableModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    MatDatepickerModule,
  ],
  templateUrl: './fee-rates.component.html',
})
export class FeeRatesComponent {
  private readonly http = inject(HttpClient);
  private readonly formBuilder = inject(FormBuilder);

  protected readonly rates = signal<FeeRate[]>([]);
  protected readonly generationResult = signal<FeeGenerationResult | null>(null);
  protected readonly busy = signal(false);
  protected readonly columns = ['effectiveMonth', 'amount', 'created'];
  protected readonly rateForm = this.formBuilder.nonNullable.group({
    effectiveMonth: [null as Date | null, Validators.required],
    amount: [null as number | null, [Validators.required, Validators.min(0.01)]],
  });
  protected readonly generateForm = this.formBuilder.nonNullable.group({
    month: [null as Date | null, Validators.required],
  });

  constructor() {
    this.reload();
  }

  protected createRate(): void {
    if (this.rateForm.invalid || this.busy()) {
      return;
    }
    this.busy.set(true);
    const { effectiveMonth, amount } = this.rateForm.getRawValue();
    this.http
      .post<FeeRate>('/api/fee-rates', {
        effectiveMonth: this.toDateOnly(effectiveMonth!),
        amount,
      })
      .subscribe({
        next: () => {
          this.busy.set(false);
          this.rateForm.reset();
          this.reload();
        },
        error: () => this.busy.set(false),
      });
  }

  protected generate(): void {
    if (this.generateForm.invalid || this.busy()) {
      return;
    }
    this.busy.set(true);
    const { month } = this.generateForm.getRawValue();
    this.http
      .post<FeeGenerationResult>('/api/fee-obligations/generate', { month: this.toDateOnly(month!) })
      .subscribe({
        next: (result) => {
          this.busy.set(false);
          this.generationResult.set(result);
        },
        error: () => this.busy.set(false),
      });
  }

  private reload(): void {
    this.http.get<FeeRate[]>('/api/fee-rates').subscribe((rates) => this.rates.set(rates));
  }

  private toDateOnly(date: Date): string {
    return date.toISOString().slice(0, 10);
  }
}
