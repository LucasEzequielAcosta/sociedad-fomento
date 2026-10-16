import { HttpErrorResponse, HttpInterceptorFn, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { MatSnackBar } from '@angular/material/snack-bar';
import { catchError, throwError } from 'rxjs';

interface ProblemDetails {
  title?: string;
  detail?: string;
  code?: string;
}

/** Shows a snackbar for failed API calls and rethrows the error. */
export const httpErrorInterceptor: HttpInterceptorFn = (req, next) => {
  const snackBar = inject(MatSnackBar);
  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== HttpStatusCode.Unauthorized) {
        const problem = error.error as ProblemDetails | undefined;
        snackBar.open(problem?.detail ?? problem?.title ?? 'Ocurrió un error inesperado.', 'Cerrar', {
          duration: 5000,
        });
      }
      return throwError(() => error);
    })
  );
};
