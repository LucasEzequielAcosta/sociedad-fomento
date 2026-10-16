import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { map } from 'rxjs';
import { PortalAuthService } from './portal-auth.service';

/** Restricts a route to authenticated members in the portal. */
export const memberGuard: CanActivateFn = () => {
  const portalAuth = inject(PortalAuthService);
  const router = inject(Router);
  return portalAuth
    .ensureSession()
    .pipe(map((identity) => (identity ? true : router.createUrlTree(['/portal']))));
};
