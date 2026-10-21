import { HttpErrorResponse } from '@angular/common/http';

/** Readable message from a failed API call: ProblemDetails 'title', or the 'errors' map of model binding. */
export function errorText(e: HttpErrorResponse): string {
  if (e.status === 0) return 'Cannot reach the API. Is the backend running?';
  if (e.status === 429) return 'Too many attempts. Please wait a minute and try again.';
  const errors = e.error?.errors ? (Object.values(e.error.errors) as string[][]).flat() : [];
  return errors.length ? errors.join(' ') : (e.error?.title ?? 'Unexpected error.');
}
