import { effect, Injectable, computed, inject, signal } from '@angular/core';
import { Subject, catchError, map, of, switchMap } from 'rxjs';
import { AppError } from '../domain/app-error';
import { Book, BookQuery, BookSortField, Genre, GenreSummary } from '../domain/models';
import { BookRepository } from '../domain/repositories';

const DEFAULT_QUERY: BookQuery = {
  genre: null,
  page: 1,
  pageSize: 10,
  sortField: 'title',
  descending: false,
};

type ListState =
  | { status: 'loading'; items: Book[]; total: number }
  | { status: 'ready'; items: Book[]; total: number }
  | { status: 'error'; items: Book[]; total: number; message: string };

/**
 * Shared application state for the book list.
 *
 * Header and Body are sibling components, so they cannot talk through @Input/@Output. Both inject this
 * root-scoped, signal-based store instead: Header writes the genre filter, Body writes paging/sorting and
 * reads the results. One source of truth, no event plumbing through the shell, trivially testable.
 */
@Injectable({ providedIn: 'root' })
export class BookListStore {
  private readonly books = inject(BookRepository);
  private readonly fetch$ = new Subject<BookQuery>();

  private readonly _query = signal<BookQuery>(DEFAULT_QUERY);
  private readonly _state = signal<ListState>({ status: 'loading', items: [], total: 0 });
  private readonly _genres = signal<GenreSummary[]>([]);

  readonly query = this._query.asReadonly();
  readonly genre = computed(() => this._query().genre);
  readonly items = computed(() => this._state().items);
  readonly total = computed(() => this._state().total);
  readonly loading = computed(() => this._state().status === 'loading');
  readonly error = computed(() => {
    const s = this._state();
    return s.status === 'error' ? s.message : null;
  });
  readonly genres = this._genres.asReadonly();

  constructor() {
    // switchMap drops stale responses when the user changes filter/page quickly.
    this.fetch$
      .pipe(
        switchMap((query) =>
          this.books.list(query).pipe(
            map((page): ListState => ({ status: 'ready', items: page.items, total: page.totalCount })),
            catchError((e: unknown) =>
              of<ListState>({ status: 'error', items: [], total: 0, message: messageOf(e) }),
            ),
          ),
        ),
      )
      .subscribe((state) => this._state.set(state));

    effect(() => this.fetch$.next(this._query()));
    this.loadGenres();
  }

  setGenre(genre: Genre | null): void {
    this.patch({ genre, page: 1 });
  }

  /** Paging and sorting arrive together from the table (it resets to page 1 itself when the sort changes). */
  setTable(page: number, pageSize: number, sortField: BookSortField, descending: boolean): void {
    this.patch({ page, pageSize, sortField, descending });
  }

  /** Re-fetch the current query and genre counts (after add / update / delete, or on retry). */
  refresh(): void {
    this._state.update((s) => ({ ...s, status: 'loading' }) as ListState);
    this.fetch$.next(this._query());
    this.loadGenres();
  }

  /** After a delete the current page may no longer exist; step back one page if it emptied out. */
  afterRemoval(): void {
    const q = this._query();
    if (this.items().length === 1 && q.page > 1) {
      this._query.set({ ...q, page: q.page - 1 });
      this.loadGenres();
    } else {
      this.refresh();
    }
  }

  private patch(change: Partial<BookQuery>): void {
    const next = { ...this._query(), ...change };
    const cur = this._query();
    if (
      next.genre === cur.genre &&
      next.page === cur.page &&
      next.pageSize === cur.pageSize &&
      next.sortField === cur.sortField &&
      next.descending === cur.descending
    ) {
      return; // PrimeNG's lazy table emits on init; don't refetch an identical query.
    }
    this._query.set(next);
  }

  private loadGenres(): void {
    this.books
      .genres()
      .pipe(catchError(() => of<GenreSummary[]>([])))
      .subscribe((g) => {
        if (g.length > 0) this._genres.set(g);
      });
  }
}

function messageOf(e: unknown): string {
  return e instanceof AppError ? e.message : 'Something went wrong while loading books.';
}
