import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthStore } from '../../auth/data/auth.store';

let refreshInFlight = false;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthStore);
  const token = auth.accessToken();

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status !== 401 || req.url.includes('/auth/login') || req.url.includes('/auth/refresh')) {
        return throwError(() => error);
      }

      if (refreshInFlight || !auth.refreshToken()) {
        auth.logout();
        return throwError(() => error);
      }

      refreshInFlight = true;
      return auth.refresh().pipe(
        switchMap(() => {
          refreshInFlight = false;
          const retry = req.clone({
            setHeaders: { Authorization: `Bearer ${auth.accessToken()}` },
          });
          return next(retry);
        }),
        catchError((refreshError) => {
          refreshInFlight = false;
          auth.logout();
          return throwError(() => refreshError);
        }),
      );
    }),
  );
};
