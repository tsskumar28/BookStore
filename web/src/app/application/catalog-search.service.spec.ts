import { TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';
import { AppError } from '../domain/app-error';
import { CatalogBook, CatalogSearchResult } from '../domain/models';
import { CatalogRepository } from '../domain/repositories';
import { CatalogSearchService } from './catalog-search.service';

const hit: CatalogBook = {
  openLibraryKey: '/works/OL1W',
  title: 'Dune',
  author: 'Frank Herbert',
  publishedYear: 1965,
  isbn: null,
  coverUrl: null,
};

class FakeCatalog extends CatalogRepository {
  terms: string[] = [];
  next: () => Observable<CatalogSearchResult> = () => of({ items: [hit], totalFound: 1 });
  search(term: string) {
    this.terms.push(term);
    return this.next();
  }
}

describe('CatalogSearchService', () => {
  let catalog: FakeCatalog;
  let service: CatalogSearchService;

  beforeEach(() => {
    catalog = new FakeCatalog();
    TestBed.configureTestingModule({
      providers: [CatalogSearchService, { provide: CatalogRepository, useValue: catalog }],
    });
    service = TestBed.inject(CatalogSearchService);
  });

  it('starts idle', () => {
    expect(service.status()).toBe('idle');
  });

  it('ignores terms shorter than two characters', () => {
    service.search(' a ');
    expect(catalog.terms).toEqual([]);
    expect(service.status()).toBe('idle');
  });

  it('exposes results on success', () => {
    service.search('  dune ');
    expect(catalog.terms).toEqual(['dune']);
    expect(service.status()).toBe('success');
    expect(service.results()).toEqual([hit]);
    expect(service.empty()).toBe(false);
  });

  it('reports the no-results case', () => {
    catalog.next = () => of({ items: [], totalFound: 0 });
    service.search('zzzz');
    expect(service.empty()).toBe(true);
  });

  it('reports a search failure and can retry', () => {
    catalog.next = () => throwError(() => new AppError('unavailable', 'Open Library is currently unavailable.'));
    service.search('dune');
    expect(service.status()).toBe('error');
    expect(service.error()).toBe('Open Library is currently unavailable.');

    catalog.next = () => of({ items: [hit], totalFound: 1 });
    service.retry();
    expect(service.status()).toBe('success');
    expect(catalog.terms).toEqual(['dune', 'dune']);
  });
});
