import { Component, inject, signal } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { provideNativeDateAdapter } from '@angular/material/core';
import { MemberService } from './member.service';

/** Member registration form. */
@Component({
  selector: 'app-member-form',
  providers: [provideNativeDateAdapter()],
  imports: [ReactiveFormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule, MatDatepickerModule],
  templateUrl: './member-form.component.html',
})
export class MemberFormComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly memberService = inject(MemberService);
  private readonly router = inject(Router);

  protected readonly busy = signal(false);
  protected readonly form = this.formBuilder.nonNullable.group({
    dni: ['', Validators.required],
    firstName: ['', Validators.required],
    lastName: ['', Validators.required],
    address: ['', Validators.required],
    birthDate: [null as Date | null, Validators.required],
    phone: ['', Validators.required],
    email: ['', Validators.email],
  });

  protected submit(): void {
    if (this.form.invalid || this.busy()) {
      return;
    }
    this.busy.set(true);
    const value = this.form.getRawValue();
    this.memberService
      .create(value.dni, {
        firstName: value.firstName,
        lastName: value.lastName,
        address: value.address,
        birthDate: value.birthDate!.toISOString().slice(0, 10),
        phone: value.phone,
        email: value.email || null,
      })
      .subscribe({
        next: (member) => this.router.navigate(['/socios', member.id]),
        error: () => this.busy.set(false),
      });
  }
}
