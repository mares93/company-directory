import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { API_BASE } from './api';
import { SessionStore } from './services/session/session';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const store = inject(SessionStore);
  const token = store.session()?.token;
  const isApi = req.url.startsWith(API_BASE);
  const authed = token && isApi ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authed).pipe(catchError((e: HttpErrorResponse) => {
    // A 401 on a company call means the token expired or was rejected; on /auth/* it only means wrong credentials.
    if (e.status === 401 && isApi && !req.url.startsWith(`${API_BASE}/auth/`)) store.expire();
    return throwError(() => e);
  }));
};
