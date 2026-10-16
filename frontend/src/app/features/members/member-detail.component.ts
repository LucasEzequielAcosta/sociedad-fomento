import { Component, inject, input, OnInit, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MemberService } from './member.service';
import { MemberDetail, MemberStatement } from '../../core/api-models';

/** Member detail with personal data, lifecycle actions and account statement. */
@Component({
  selector: 'app-member-detail',
  providers: [provideNativeDateAdapter()],
  imports: [
    CurrencyPipe,
    DatePipe,
    ReactiveFormsModule,
    RouterLink,
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule,
    MatDatepickerModule,
  ],
  templateUrl: './member-detail.component.html',
})
export class MemberDetailComponent implements OnInit {
  private readonly formBuilder = inject(FormBuilder);
  private readonly memberService = inject(MemberService);

  readonly id = input.required<string>();

  protected readonly member = signal<MemberDetail | null>(null);
  protected readonly statement = signal<MemberStatement | null>(null);
  protected readonly editing = signal(false);
  protected readonly confirming = signal<'deactivate' | 'reactivate' | null>(null);
  protected readonly obligationColumns = ['period', 'amount', 'status'];
  protected readonly paymentColumns = ['date', 'amount', 'method', 'status'];
  protected readonly form = this.formBuilder.nonNullable.group({
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    address: ['', Validators.required],
    birthDate: [null as Date | null, Validators.required],
    phone: ['', Validators.required],
    email: ['', Validators.email],
  });

  ngOnInit(): void {
    const memberId = Number(this.id());
    this.memberService.getById(memberId).subscribe((member) => {
      this.member.set(member);
      this.form.patchValue({
        firstName: member.firstName,
        lastName: member.lastName,
        address: member.address,
        birthDate: new Date(`${member.birthDate}T00:00:00`),
        phone: member.phone,
        email: member.email ?? '',
      });
    });
    this.reloadStatement(memberId);
  }

  protected savePersonalData(): void {
    if (this.form.invalid) {
      return;
    }
    const value = this.form.getRawValue();
    this.memberService
      .updatePersonalData(this.member()!.id, {
        firstName: value.firstName,
        lastName: value.lastName,
        address: value.address,
        birthDate: value.birthDate!.toISOString().slice(0, 10),
        phone: value.phone,
        email: value.email || null,
      })
      .subscribe((member) => {
        this.member.set(member);
        this.editing.set(false);
      });
  }

  protected confirm(action: 'deactivate' | 'reactivate'): void {
    const memberId = this.member()!.id;
    const after = (): void => {
      this.confirming.set(null);
      this.memberService.getById(memberId).subscribe((member) => this.member.set(member));
      this.reloadStatement(memberId);
    };
    if (action === 'deactivate') {
      this.memberService.deactivate(memberId).subscribe(after);
    } else {
      this.memberService.reactivate(memberId).subscribe(after);
    }
  }

  protected statusLabel(status: string): string {
    const labels: Record<string, string> = {
      Pending: 'Pendiente',
      Paid: 'Pagada',
      Overdue: 'Vencida',
      Future: 'Futura',
      Active: 'Activo',
      Cancelled: 'Anulado',
    };
    return labels[status] ?? status;
  }

  protected methodLabel(method: string): string {
    const labels: Record<string, string> = {
      Cash: 'Efectivo',
      BankTransfer: 'Transferencia',
      DirectDebit: 'Débito automático',
    };
    return labels[method] ?? method;
  }

  private reloadStatement(memberId: number): void {
    this.memberService.getStatement(memberId).subscribe((statement) => this.statement.set(statement));
  }
}
