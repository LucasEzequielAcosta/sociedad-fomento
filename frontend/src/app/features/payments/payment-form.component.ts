import { Component, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { MatButtonModule } from '@angular/material/button';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MemberService } from '../members/member.service';
import { MemberDetail, MemberStatement, PaymentMethod } from '../../core/api-models';

/** Manual payment registration: member lookup, obligation selection and amount check. */
@Component({
  selector: 'app-payment-form',
  providers: [provideNativeDateAdapter()],
  imports: [
    CurrencyPipe,
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatSelectModule,
    MatButtonModule,
    MatCheckboxModule,
    MatDatepickerModule,
  ],
  templateUrl: './payment-form.component.html',
})
export class PaymentFormComponent {
  private readonly http = inject(HttpClient);
  private readonly formBuilder = inject(FormBuilder);
  private readonly memberService = inject(MemberService);
  private readonly router = inject(Router);

  protected readonly member = signal<MemberDetail | null>(null);
  protected readonly statement = signal<MemberStatement | null>(null);
  protected readonly memberNotFound = signal(false);
  protected readonly selectedIds = signal<Set<number>>(new Set());
  protected readonly busy = signal(false);
  protected readonly dniForm = this.formBuilder.nonNullable.group({ dni: ['', Validators.required] });
  protected readonly paymentForm = this.formBuilder.nonNullable.group({
    paymentDate: [new Date(), Validators.required],
    method: ['Cash' as PaymentMethod, Validators.required],
  });
  protected readonly total = computed(() => {
    const ids = this.selectedIds();
    return (this.statement()?.obligations ?? [])
      .filter((obligation) => ids.has(obligation.id))
      .reduce((sum, obligation) => sum + obligation.amount, 0);
  });

  protected payableObligations() {
    return (this.statement()?.obligations ?? []).filter((obligation) => obligation.status !== 'Paid');
  }

  protected findMember(): void {
    if (this.dniForm.invalid) {
      return;
    }
    this.memberNotFound.set(false);
    this.memberService.getByDni(this.dniForm.getRawValue().dni).subscribe({
      next: (member) => {
        this.member.set(member);
        this.selectedIds.set(new Set());
        this.memberService.getStatement(member.id).subscribe((statement) => this.statement.set(statement));
      },
      error: () => {
        this.member.set(null);
        this.statement.set(null);
        this.memberNotFound.set(true);
      },
    });
  }

  protected toggle(id: number, checked: boolean): void {
    const next = new Set(this.selectedIds());
    if (checked) {
      next.add(id);
    } else {
      next.delete(id);
    }
    this.selectedIds.set(next);
  }

  protected submit(): void {
    const member = this.member();
    const ids = [...this.selectedIds()];
    if (!member || ids.length === 0 || this.paymentForm.invalid || this.busy()) {
      return;
    }
    this.busy.set(true);
    const { paymentDate, method } = this.paymentForm.getRawValue();
    this.http
      .post('/api/payments', {
        memberId: member.id,
        paymentDate: paymentDate.toISOString().slice(0, 10),
        amount: this.total(),
        method,
        obligationIds: ids,
      })
      .subscribe({
        next: () => this.router.navigate(['/pagos']),
        error: () => this.busy.set(false),
      });
  }
}
