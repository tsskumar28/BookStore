import {
  Book,
  CatalogBook,
  CatalogSearchResult,
  Genre,
  GenreSummary,
  NewBook,
  Page,
  StoreFields,
} from '../domain/models';
import {
  BookDto,
  CatalogBookDto,
  CatalogSearchResultDto,
  CreateBookRequestDto,
  GenreSummaryDto,
  PagedDto,
  UpdateBookRequestDto,
} from './api.dto';

export const toBook = (d: BookDto): Book => ({ ...d, genre: d.genre as Genre });

export const toPage = (d: PagedDto<BookDto>): Page<Book> => ({ ...d, items: d.items.map(toBook) });

export const toGenreSummary = (d: GenreSummaryDto): GenreSummary => ({
  genre: d.genre as Genre,
  count: d.count,
});

export const toCatalogBook = (d: CatalogBookDto): CatalogBook => ({ ...d });

export const toCatalogResult = (d: CatalogSearchResultDto): CatalogSearchResult => ({
  items: d.items.map(toCatalogBook),
  totalFound: d.totalFound,
});

export const toCreateRequest = (b: NewBook): CreateBookRequestDto => ({
  openLibraryKey: b.openLibraryKey,
  title: b.title,
  author: b.author,
  publishedYear: b.publishedYear,
  isbn: b.isbn,
  coverUrl: b.coverUrl,
  genre: b.genre,
  price: b.price,
  stock: b.stock,
  description: b.description,
});

export const toUpdateRequest = (f: StoreFields): UpdateBookRequestDto => ({ ...f });
