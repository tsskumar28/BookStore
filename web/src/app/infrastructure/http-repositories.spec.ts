import { HttpErrorResponse, provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AppError } from '../domain/app-error';
import { HttpBookRepository, HttpCatalogRepository } from './http-repositories';
import { toAppError } from './map-http-error';

describe('HttpBookRepository', () => {
  let repo: HttpBookRepository;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting(), HttpBookRepository, HttpCatalogRepository],
    });
    repo = TestBed.inject(HttpBookRepository);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends genre, paging and sorting as query params', () => {
    repo.list({ genre: 'SciFi', page: 2, pageSize: 5, sortField: 'price', descending: true }).subscribe();

    const req = http.expectOne((r) => r.url === '/api/books');
    expect(req.request.params.get('genre')).toBe('SciFi');
    expect(req.request.params.get('page')).toBe('2');
    expect(req.request.params.get('pageSize')).toBe('5');
    expect(req.request.params.get('sortBy')).toBe('price');
    expect(req.request.params.get('desc')).toBe('true');
    req.flush({ items: [], page: 2, pageSize: 5, totalCount: 0 });
  });

  it('omits the genre param for "All"', () => {
    repo.list({ genre: null, page: 1, pageSize: 10, sortField: 'title', descending: false }).subscribe();
    const req = http.expectOne((r) => r.url === '/api/books');
    expect(req.request.params.has('genre')).toBe(false);
    req.flush({ items: [], page: 1, pageSize: 10, totalCount: 0 });
  });

  it('posts a selected catalog book with its store fields', () => {
    repo
      .add({
        openLibraryKey: '/works/OL1W',
        title: 'Dune',
        author: 'Frank Herbert',
        publishedYear: 1965,
        isbn: '1',
        coverUrl: null,
        genre: 'SciFi',
        price: 9.99,
        stock: 3,
        description: null,
      })
      .subscribe();

    const req = http.expectOne('/api/books');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toMatchObject({ openLibraryKey: '/works/OL1W', genre: 'SciFi', price: 9.99 });
    req.flush({});
  });

  it('translates a 409 into a conflict AppError', () => {
    let error: unknown;
    repo.delete('1').subscribe({ error: (e) => (error = e) });
    http.expectOne('/api/books/1').flush({ detail: 'Already there' }, { status: 409, statusText: 'Conflict' });

    expect(error).toBeInstanceOf(AppError);
    expect((error as AppError).kind).toBe('conflict');
    expect((error as AppError).message).toBe('Already there');
  });

  it('calls the proxied catalog search endpoint', () => {
    TestBed.inject(HttpCatalogRepository).search('dune').subscribe();
    const req = http.expectOne((r) => r.url === '/api/catalog/search');
    expect(req.request.params.get('q')).toBe('dune');
    req.flush({ items: [], totalFound: 0 });
  });
});

describe('toAppError', () => {
  const http = (status: number, error: unknown = null) => new HttpErrorResponse({ status, error });

  it('maps validation problem details to camelCase field errors', () => {
    const e = toAppError(http(400, { title: 'Validation failed', errors: { Price: ['Too low'] } }));
    expect(e.kind).toBe('validation');
    expect(e.fieldErrors).toEqual({ price: ['Too low'] });
  });

  it('maps connection failures and upstream outages to distinct kinds', () => {
    expect(toAppError(http(0)).kind).toBe('network');
    expect(toAppError(http(502, { detail: 'Open Library timed out.' })).kind).toBe('unavailable');
    expect(toAppError(http(500)).kind).toBe('unknown');
  });
});
