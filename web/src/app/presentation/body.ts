import { CurrencyPipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { ConfirmationService, MessageService } from 'primeng/api';
import { Button } from 'primeng/button';
import { ConfirmDialog } from 'primeng/confirmdialog';
import { Message } from 'primeng/message';
import { TableLazyLoadEvent, TableModule } from 'primeng/table';
import { Tag } from 'primeng/tag';
import { Toast } from 'primeng/toast';
import { BookListStore } from '../application/book-list.store';
import { DeleteBookUseCase } from '../application/book-use-cases';
import { AppError } from '../domain/app-error';
import { Book, BookSortField } from '../domain/models';
import { AddBookDialog } from './add-book-dialog';
import { BookCover } from './book-cover';
import { EditBookDialog } from './edit-book-dialog';
import { genreLabel } from './genre-label';

/** The books grid. Reads results and writes paging/sorting through the shared {@link BookListStore}. */
@Component({
  selector: 'app-body',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CurrencyPipe,
    TableModule,
    Button,
    Tag,
    Message,
    Toast,
    ConfirmDialog,
    BookCover,
    AddBookDialog,
    EditBookDialog,
  ],
  template: `
    <main class="mx-auto w-full max-w-6xl flex-1 px-4 py-6">
      <div class="mb-4 flex items-center justify-between gap-3">
        <h2 class="m-0 text-lg font-semibold">
          Books <span class="text-sm font-normal text-muted-color">({{ store.total() }})</span>
        </h2>
        <p-button label="Add book" icon="pi pi-plus" (onClick)="adding.set(true)" />
      </div>

      @if (store.error(); as message) {
        <p-message severity="error" styleClass="mb-4 w-full">
          <div class="flex w-full items-center justify-between gap-3">
            <span>{{ message }}</span>
            <p-button label="Retry" size="small" severity="secondary" (onClick)="store.refresh()" />
          </div>
        </p-message>
      }

      <p-table
        [value]="store.items()"
        [lazy]="true"
        (onLazyLoad)="onLazyLoad($event)"
        [loading]="store.loading()"
        [paginator]="true"
        [rows]="store.query().pageSize"
        [first]="(store.query().page - 1) * store.query().pageSize"
        [totalRecords]="store.total()"
        [rowsPerPageOptions]="[5, 10, 25]"
        [sortField]="store.query().sortField"
        [sortOrder]="store.query().descending ? -1 : 1"
        dataKey="id"
        [tableStyle]="{ 'min-width': '48rem' }"
        [rowHover]="true"
        [scrollable]="false"
      >
        <ng-template #header>
          <tr>
            <th class="w-16"></th>
            <th pSortableColumn="title">Title <p-sortIcon field="title" /></th>
            <th pSortableColumn="author">Author <p-sortIcon field="author" /></th>
            <th pSortableColumn="genre">Genre <p-sortIcon field="genre" /></th>
            <th pSortableColumn="publishedYear">Year <p-sortIcon field="publishedYear" /></th>
            <th pSortableColumn="price" class="text-right">Price <p-sortIcon field="price" /></th>
            <th pSortableColumn="stock" class="text-right">Stock <p-sortIcon field="stock" /></th>
            <th class="w-28"><span class="sr-only">Actions</span></th>
          </tr>
        </ng-template>

        <ng-template #body let-book>
          <tr>
            <td><app-book-cover [src]="book.coverUrl" [title]="book.title" /></td>
            <td class="font-medium">{{ book.title }}</td>
            <td>{{ book.author }}</td>
            <td><p-tag [value]="label(book.genre)" severity="secondary" /></td>
            <td>{{ book.publishedYear ?? '—' }}</td>
            <td class="text-right">{{ book.price | currency }}</td>
            <td class="text-right">
              @if (book.stock === 0) {
                <p-tag value="Out of stock" severity="danger" />
              } @else {
                {{ book.stock }}
              }
            </td>
            <td class="text-right whitespace-nowrap">
              <p-button icon="pi pi-pencil" [text]="true" [rounded]="true" [ariaLabel]="'Edit ' + book.title" (onClick)="editing.set(book)" />
              <p-button icon="pi pi-trash" [text]="true" [rounded]="true" severity="danger" [ariaLabel]="'Delete ' + book.title" (onClick)="confirmDelete(book)" />
            </td>
          </tr>
        </ng-template>

        <ng-template #emptymessage>
          <tr>
            <td colspan="8" class="py-10 text-center text-muted-color">
              @if (store.error()) {
                Books could not be loaded.
              } @else if (store.genre()) {
                No books in this genre yet.
              } @else {
                The store is empty. Add your first book!
              }
            </td>
          </tr>
        </ng-template>
      </p-table>
    </main>

    <p-toast position="bottom-right" />
    <p-confirmdialog />

    @if (adding()) {
      <app-add-book-dialog (closed)="adding.set(false)" />
    }
    @if (editing(); as book) {
      <app-edit-book-dialog [book]="book" (closed)="editing.set(null)" />
    }
  `,
})
export class Body {
  protected readonly store = inject(BookListStore);
  private readonly deleteBook = inject(DeleteBookUseCase);
  private readonly confirm = inject(ConfirmationService);
  private readonly toast = inject(MessageService);

  protected readonly adding = signal(false);
  protected readonly editing = signal<Book | null>(null);
  protected readonly label = genreLabel;

  protected onLazyLoad(e: TableLazyLoadEvent): void {
    const rows = e.rows ?? 10;
    const first = e.first ?? 0;
    const field = (typeof e.sortField === 'string' ? e.sortField : 'title') as BookSortField;
    this.store.setTable(Math.floor(first / rows) + 1, rows, field, e.sortOrder === -1);
  }

  protected confirmDelete(book: Book): void {
    this.confirm.confirm({
      header: 'Delete book',
      message: `Remove “${book.title}” from the store?`,
      icon: 'pi pi-exclamation-triangle',
      acceptButtonProps: { label: 'Delete', severity: 'danger' },
      rejectButtonProps: { label: 'Cancel', severity: 'secondary', outlined: true },
      accept: () =>
        this.deleteBook.execute(book.id).subscribe({
          next: () => this.toast.add({ severity: 'success', summary: 'Book deleted', detail: book.title }),
          error: (e: unknown) =>
            this.toast.add({
              severity: 'error',
              summary: 'Delete failed',
              detail: e instanceof AppError ? e.message : 'Please try again.',
            }),
        }),
    });
  }
}
