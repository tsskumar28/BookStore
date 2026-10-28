import { Injectable, computed, inject, signal } from '@angular/core';
import { Subject, catchError, map, of, switchMap, tap } from 'rxjs';
import { AppError } from '../domain/app-error';
import { CatalogBook } from '../domain/models';
import { CatalogRepository } from '../domain/repositories';

export type SearchStatus = 'idle' | 'loading' | 'success' | 'error';

/**
 * Search-the-catalog use case with its transient state. Provided per dialog (not root) so each
 * "Add book" session starts clean; results never outlive it.
 */
@Injectable()
export class CatalogSearchService {
  private readonly catalog = inject(CatalogRepository);
  private readonly term$ = new Subject<string>();

  private readonly _status = signal<SearchStatus>('idle');
  private readonly _results = signal<CatalogBook[]>([]);
  private readonly _error = signal<string | null>(null);
  private lastTerm = '';

  readonly status = this._status.asReadonly();
  readonly results = this._results.asReadonly();
  readonly error = this._error.asReadonly();
  readonly empty = computed(() => this._status() === 'success' && this._results().length === 0);

  constructor() {
    this.term$
      .pipe(
        tap(() => {
          this._status.set('loading');
          this._error.set(null);
        }),
        switchMap((term) =>
          this.catalog.search(term).pipe(
            map((r) => ({ ok: true as const, items: r.items })),
            catchError((e: unknown) => of({ ok: false as const, message: messageOf(e) })),
          ),
        ),
      )
      .subscribe((outcome) => {
        if (outcome.ok) {
          this._results.set(outcome.items);
          this._status.set('success');
        } else {
          this._results.set([]);
          this._error.set(outcome.message);
          this._status.set('error');
        }
      });
  }

  search(term: string): void {
    const trimmed = term.trim();
    if (trimmed.length < 2) return;
    this.lastTerm = trimmed;
    this.term$.next(trimmed);
  }

  retry(): void {
    if (this.lastTerm) this.term$.next(this.lastTerm);
  }
}

function messageOf(e: unknown): string {
  return e instanceof AppError ? e.message : 'The search failed. Please try again.';
}
