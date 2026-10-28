import { Injectable, inject } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { Book, NewBook, StoreFields } from '../domain/models';
import { BookRepository } from '../domain/repositories';
import { BookListStore } from './book-list.store';

/** Add a catalog result (plus store-owned fields) to the store, then refresh the list. */
@Injectable({ providedIn: 'root' })
export class AddBookUseCase {
  private readonly books = inject(BookRepository);
  private readonly store = inject(BookListStore);

  execute(book: NewBook): Observable<Book> {
    return this.books.add(book).pipe(tap(() => this.store.refresh()));
  }
}

@Injectable({ providedIn: 'root' })
export class UpdateBookUseCase {
  private readonly books = inject(BookRepository);
  private readonly store = inject(BookListStore);

  execute(id: string, fields: StoreFields): Observable<Book> {
    return this.books.update(id, fields).pipe(tap(() => this.store.refresh()));
  }
}

@Injectable({ providedIn: 'root' })
export class DeleteBookUseCase {
  private readonly books = inject(BookRepository);
  private readonly store = inject(BookListStore);

  execute(id: string): Observable<void> {
    return this.books.delete(id).pipe(tap(() => this.store.afterRemoval()));
  }
}
