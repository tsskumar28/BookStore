/** Wire formats of the BookStore API. Only the infrastructure layer knows these. */

export interface BookDto {
  id: string;
  title: string;
  author: string;
  isbn: string | null;
  publishedYear: number | null;
  coverUrl: string | null;
  openLibraryKey: string;
  description: string | null;
  genre: string;
  price: number;
  stock: number;
}

export interface PagedDto<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface GenreSummaryDto {
  genre: string;
  count: number;
}

export interface CatalogBookDto {
  openLibraryKey: string;
  title: string;
  author: string;
  publishedYear: number | null;
  isbn: string | null;
  coverUrl: string | null;
}

export interface CatalogSearchResultDto {
  items: CatalogBookDto[];
  totalFound: number;
}

export interface CreateBookRequestDto extends CatalogBookDto {
  description: string | null;
  genre: string;
  price: number;
  stock: number;
}

export interface UpdateBookRequestDto {
  genre: string;
  price: number;
  stock: number;
  description: string | null;
}
