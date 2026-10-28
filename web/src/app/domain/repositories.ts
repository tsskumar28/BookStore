import { Observable } from 'rxjs';
import {
  Book,
  BookQuery,
  CatalogSearchResult,
  GenreSummary,
  NewBook,
  Page,
  StoreFields,
} from './models';

/**
 * Ports. Abstract classes (not interfaces) so they double as DI tokens while staying framework-free.
 * Implementations live in the infrastructure layer; failures surface as {@link AppError}.
 */
export abstract class BookRepository {
  abstract list(query: BookQuery): Observable<Page<Book>>;
  abstract genres(): Observable<GenreSummary[]>;
  abstract add(book: NewBook): Observable<Book>;
  abstract update(id: string, fields: StoreFields): Observable<Book>;
  abstract delete(id: string): Observable<void>;
}

export abstract class CatalogRepository {
  abstract search(term: string): Observable<CatalogSearchResult>;
}
