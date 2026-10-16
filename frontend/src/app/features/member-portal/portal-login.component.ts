import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonModule } from '@angular/material/button';
import { PortalAuthService } from '../../core/portal-auth.service';

/** Member portal login using only the member DNI. */
@Component({
  selector: 'app-portal-login',
  imports: [ReactiveFormsModule, RouterLink, MatCardModule, MatFormFieldModule, MatInputModule, MatButtonModule],
  templateUrl: './portal-login.component.html',
})
export class PortalLoginComponent {
  private readonly formBuilder = inject(FormBuilder);
  private readonly portalAuth = inject(PortalAuthService);
  private readonly router = inject(Router);

  protected readonly busy = signal(false);
  protected readonly errorMessage = signal<string | null>(null);
  protected readonly form = this.formBuilder.nonNullable.group({ dni: ['', Validators.required] });

  protected submit(): void {
    if (this.form.invalid || this.busy()) {
      return;
    }
    this.busy.set(true);
    this.errorMessage.set(null);
    this.portalAuth.login(this.form.getRawValue().dni).subscribe({
      next: () => this.router.navigate(['/portal/estado']),
      error: (error: HttpErrorResponse) => {
        this.busy.set(false);
        this.errorMessage.set(
          error.status === HttpStatusCode.TooManyRequests
            ? 'Demasiados intentos. Esperá un minuto y volvé a intentar.'
            : 'No se encontró un socio con ese DNI.'
        );
      },
    });
  }
}
