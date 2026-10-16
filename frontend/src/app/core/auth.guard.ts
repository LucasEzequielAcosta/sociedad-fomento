import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { AuthService } from './auth.service';

/** Restricts a route to authenticated administrators. */
export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const router = inject(Router);
  return authService
    .ensureSession()
    .pipe(map((identity) => (identity ? true : router.createUrlTree(['/login']))));
};
