import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, InjectionToken, inject } from '@angular/core';
import { Observable, map } from 'rxjs';
import {
  Book,
  BookQuery,
  CatalogSearchResult,
  GenreSummary,
  NewBook,
  Page,
  StoreFields,
} from '../domain/models';
import { BookRepository, CatalogRepository } from '../domain/repositories';
import {
  BookDto,
  CatalogSearchResultDto,
  GenreSummaryDto,
  PagedDto,
} from './api.dto';
import { mapHttpError } from './map-http-error';
import {
  toBook,
  toCatalogResult,
  toCreateRequest,
  toGenreSummary,
  toPage,
  toUpdateRequest,
} from './mappers';

/** Base URL of the API. '/api' works with the dev proxy (see proxy.conf.json); override for other hosts. */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '/api',
});

@Injectable()
export class HttpBookRepository extends BookRepository {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}/books`;

  list(q: BookQuery): Observable<Page<Book>> {
    let params = new HttpParams()
      .set('page', q.page)
      .set('pageSize', q.pageSize)
      .set('sortBy', q.sortField)
      .set('desc', q.descending);
    if (q.genre) params = params.set('genre', q.genre);

    return this.http.get<PagedDto<BookDto>>(this.url, { params }).pipe(map(toPage), mapHttpError());
  }

  genres(): Observable<GenreSummary[]> {
    return this.http
      .get<GenreSummaryDto[]>(`${this.url}/genres`)
      .pipe(map((g) => g.map(toGenreSummary)), mapHttpError());
  }

  add(book: NewBook): Observable<Book> {
    return this.http.post<BookDto>(this.url, toCreateRequest(book)).pipe(map(toBook), mapHttpError());
  }

  update(id: string, fields: StoreFields): Observable<Book> {
    return this.http.put<BookDto>(`${this.url}/${id}`, toUpdateRequest(fields)).pipe(map(toBook), mapHttpError());
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.url}/${id}`).pipe(mapHttpError());
  }
}

@Injectable()
export class HttpCatalogRepository extends CatalogRepository {
  private readonly http = inject(HttpClient);
  private readonly url = `${inject(API_BASE_URL)}/catalog`;

  search(term: string): Observable<CatalogSearchResult> {
    const params = new HttpParams().set('q', term);
    return this.http
      .get<CatalogSearchResultDto>(`${this.url}/search`, { params })
      .pipe(map(toCatalogResult), mapHttpError());
  }
}
