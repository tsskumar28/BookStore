/** Framework-agnostic domain types. No Angular or HTTP imports allowed in this layer. */

export const GENRES = [
  'Fiction',
  'SciFi',
  'Fantasy',
  'Mystery',
  'Biography',
  'NonFiction',
  'Children',
  'History',
] as const;

export type Genre = (typeof GENRES)[number];

export interface Book {
  id: string;
  title: string;
  author: string;
  isbn: string | null;
  publishedYear: number | null;
  coverUrl: string | null;
  openLibraryKey: string;
  description: string | null;
  genre: Genre;
  price: number;
  stock: number;
}

/** Store-owned fields: the only things the user supplies when adding, and may change when editing. */
export interface StoreFields {
  genre: Genre;
  price: number;
  stock: number;
  description: string | null;
}

/** A transient Open Library search hit. Not persisted until the user selects it and submits. */
export interface CatalogBook {
  openLibraryKey: string;
  title: string;
  author: string;
  publishedYear: number | null;
  isbn: string | null;
  coverUrl: string | null;
}

export type NewBook = CatalogBook & StoreFields;

export type BookSortField = 'title' | 'author' | 'genre' | 'price' | 'stock' | 'publishedYear';

export interface BookQuery {
  genre: Genre | null;
  /** 1-based. */
  page: number;
  pageSize: number;
  sortField: BookSortField;
  descending: boolean;
}

export interface Page<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface GenreSummary {
  genre: Genre;
  count: number;
}

export interface CatalogSearchResult {
  items: CatalogBook[];
  totalFound: number;
}
