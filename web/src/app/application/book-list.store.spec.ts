import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { AppError } from '../domain/app-error';
import { Book, BookQuery, Page } from '../domain/models';
import { BookRepository } from '../domain/repositories';
import { BookListStore } from './book-list.store';

const book = (title: string): Book => ({
  id: title,
  title,
  author: 'A',
  isbn: null,
  publishedYear: null,
  coverUrl: null,
  openLibraryKey: `/works/${title}`,
  description: null,
  genre: 'Fiction',
  price: 1,
  stock: 1,
});

class FakeBookRepository extends BookRepository {
  queries: BookQuery[] = [];
  fail = false;

  list(query: BookQuery): Observable<Page<Book>> {
    this.queries.push(query);
    if (this.fail) return throwError(() => new AppError('network', 'Cannot reach the server.'));
    return of({ items: [book('One'), book('Two')], page: query.page, pageSize: query.pageSize, totalCount: 42 });
  }
  genres = () => of([{ genre: 'Fiction' as const, count: 2 }]);
  add = () => of(book('x'));
  update = () => of(book('x'));
  delete = () => of(undefined);
}

describe('BookListStore', () => {
  let repo: FakeBookRepository;
  let store: BookListStore;

  beforeEach(() => {
    repo = new FakeBookRepository();
    TestBed.configureTestingModule({ providers: [{ provide: BookRepository, useValue: repo }] });
    store = TestBed.inject(BookListStore);
    TestBed.tick();
  });

  it('loads the first page and genre counts on creation', () => {
    expect(repo.queries).toHaveLength(1);
    expect(store.items().map((b) => b.title)).toEqual(['One', 'Two']);
    expect(store.total()).toBe(42);
    expect(store.genres()).toEqual([{ genre: 'Fiction', count: 2 }]);
  });

  it('setGenre (what the Header does) refetches from page 1 for the Body to render', () => {
    store.setTable(3, 10, 'title', false);
    TestBed.tick();

    store.setGenre('SciFi');
    TestBed.tick();

    const last = repo.queries.at(-1)!;
    expect(last.genre).toBe('SciFi');
    expect(last.page).toBe(1);
    expect(store.genre()).toBe('SciFi');
  });

  it('does not refetch when the table re-emits an identical query', () => {
    const before = repo.queries.length;
    store.setTable(1, 10, 'title', false);
    TestBed.tick();
    expect(repo.queries.length).toBe(before);
  });

  it('passes sorting and paging through to the repository', () => {
    store.setTable(2, 5, 'price', true);
    TestBed.tick();
    expect(repo.queries.at(-1)).toMatchObject({ page: 2, pageSize: 5, sortField: 'price', descending: true });
  });

  it('exposes a friendly error when loading fails and recovers on refresh', () => {
    repo.fail = true;
    store.refresh();
    expect(store.error()).toBe('Cannot reach the server.');
    expect(store.items()).toEqual([]);

    repo.fail = false;
    store.refresh();
    expect(store.error()).toBeNull();
    expect(store.items()).toHaveLength(2);
  });

  it('steps back a page when deleting the last item on a later page', () => {
    store.setTable(2, 10, 'title', false);
    TestBed.tick();
    // Make the page look like it has a single item left.
    repo.list = (q) => {
      repo.queries.push(q);
      return of({ items: [book('Only')], page: q.page, pageSize: q.pageSize, totalCount: 11 });
    };
    store.refresh();

    store.afterRemoval();
    TestBed.tick();

    expect(store.query().page).toBe(1);
  });
});
