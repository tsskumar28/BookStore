import { HttpErrorResponse } from '@angular/common/http';
import { Observable, OperatorFunction, catchError, throwError } from 'rxjs';
import { AppError } from '../domain/app-error';

/** Translates HttpClient failures (RFC 7807 problem details from the API) into transport-neutral AppErrors. */
export function mapHttpError<T>(): OperatorFunction<T, T> {
  return (source: Observable<T>) =>
    source.pipe(catchError((e: unknown) => throwError(() => toAppError(e))));
}

export function toAppError(e: unknown): AppError {
  if (!(e instanceof HttpErrorResponse)) return new AppError('unknown', 'Unexpected error.');

  const problem = (e.error ?? {}) as { title?: string; detail?: string; errors?: Record<string, string[]> };

  switch (e.status) {
    case 0:
      return new AppError('network', 'Cannot reach the server. Check that the API is running.');
    case 400:
      return new AppError('validation', problem.title ?? 'Validation failed', camelCaseKeys(problem.errors));
    case 404:
      return new AppError('notFound', problem.detail ?? 'The item no longer exists.');
    case 409:
      return new AppError('conflict', problem.detail ?? 'That book already exists.');
    case 502:
    case 503:
    case 504:
      return new AppError('unavailable', problem.detail ?? 'The book catalog is currently unavailable.');
    default:
      return new AppError('unknown', 'The server hit an unexpected error.');
  }
}

function camelCaseKeys(errors: Record<string, string[]> | undefined): Record<string, string[]> {
  return Object.fromEntries(
    Object.entries(errors ?? {}).map(([k, v]) => [k.charAt(0).toLowerCase() + k.slice(1), v]),
  );
}
