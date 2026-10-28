import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';
import { FormControl, ReactiveFormsModule, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { AutoFocus } from 'primeng/autofocus';
import { Button } from 'primeng/button';
import { Dialog } from 'primeng/dialog';
import { InputText } from 'primeng/inputtext';
import { Message } from 'primeng/message';
import { ProgressSpinner } from 'primeng/progressspinner';
import { AddBookUseCase } from '../application/book-use-cases';
import { CatalogSearchService } from '../application/catalog-search.service';
import { AppError } from '../domain/app-error';
import { CatalogBook, StoreFields } from '../domain/models';
import { BookCover } from './book-cover';
import { StoreFieldsForm } from './store-fields-form';

/**
 * Two-step add flow: (1) search Open Library via our API and pick a result, then
 * (2) supply the store-owned fields and save. Rendered fresh each time it is opened.
 */
@Component({
  selector: 'app-add-book-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [CatalogSearchService],
  imports: [ReactiveFormsModule, AutoFocus, Dialog, Button, InputText, Message, ProgressSpinner, BookCover, StoreFieldsForm],
  template: `
    <p-dialog
      [header]="selected() ? 'Set price & genre' : 'Add a book'"
      [(visible)]="visible"
      [modal]="true"
      [draggable]="false"
      [style]="{ width: '40rem' }"
      [breakpoints]="{ '700px': '95vw' }"
      (onHide)="closed.emit()"
    >
      @if (!selected()) {
        <form class="flex gap-2" (submit)="runSearch($event)">
          <input
            pInputText
            type="search"
            class="flex-1"
            [formControl]="term"
            placeholder="Search by title, author or ISBN"
            aria-label="Search Open Library"
            [pAutoFocus]="true"
          />
          <p-button type="submit" icon="pi pi-search" label="Search" [disabled]="term.invalid" [loading]="search.status() === 'loading'" />
        </form>

        <div class="mt-4 min-h-40" aria-live="polite">
          @switch (search.status()) {
            @case ('idle') {
              <p class="py-8 text-center text-muted-color">Search the Open Library catalog to find a book to add.</p>
            }
            @case ('loading') {
              <div class="flex justify-center py-10"><p-progress-spinner strokeWidth="4" styleClass="h-10 w-10" /></div>
            }
            @case ('error') {
              <p-message severity="error" styleClass="w-full">
                <div class="flex w-full items-center justify-between gap-3">
                  <span>{{ search.error() }}</span>
                  <p-button label="Retry" size="small" severity="secondary" (onClick)="search.retry()" />
                </div>
              </p-message>
            }
            @case ('success') {
              @if (search.empty()) {
                <p class="py-8 text-center text-muted-color">No results for “{{ term.value }}”. Try a different search.</p>
              } @else {
                <ul class="m-0 flex max-h-96 list-none flex-col gap-1 overflow-y-auto p-0">
                  @for (r of search.results(); track r.openLibraryKey) {
                    <li>
                      <button
                        type="button"
                        class="flex w-full cursor-pointer items-center gap-3 rounded border-0 bg-transparent p-2 text-left hover:bg-emphasis"
                        (click)="select(r)"
                      >
                        <app-book-cover [src]="r.coverUrl" [title]="r.title" />
                        <span class="flex min-w-0 flex-col">
                          <span class="truncate font-semibold">{{ r.title }}</span>
                          <span class="truncate text-sm text-muted-color">
                            {{ r.author }}@if (r.publishedYear) { · {{ r.publishedYear }} }
                          </span>
                        </span>
                      </button>
                    </li>
                  }
                </ul>
              }
            }
          }
        </div>
      } @else {
        <div class="mb-4 flex items-center gap-3 rounded bg-emphasis p-3">
          <app-book-cover [src]="selected()!.coverUrl" [title]="selected()!.title" />
          <div class="flex min-w-0 flex-1 flex-col">
            <span class="truncate font-semibold">{{ selected()!.title }}</span>
            <span class="truncate text-sm text-muted-color">{{ selected()!.author }}</span>
          </div>
          <p-button label="Change" size="small" severity="secondary" [text]="true" [disabled]="saving()" (onClick)="selected.set(null)" />
        </div>

        @if (saveError(); as msg) {
          <p-message severity="error" styleClass="mb-4 w-full">{{ msg }}</p-message>
        }

        <app-store-fields-form
          submitLabel="Add to store"
          [busy]="saving()"
          [serverErrors]="fieldErrors()"
          (submitted)="save($event)"
          (cancelled)="visible.set(false)"
        />
      }
    </p-dialog>
  `,
})
export class AddBookDialog {
  protected readonly search = inject(CatalogSearchService);
  private readonly addBook = inject(AddBookUseCase);
  private readonly toast = inject(MessageService);

  readonly closed = output<void>();

  protected readonly visible = signal(true);
  protected readonly selected = signal<CatalogBook | null>(null);
  protected readonly saving = signal(false);
  protected readonly saveError = signal<string | null>(null);
  protected readonly fieldErrors = signal<Record<string, string[]>>({});

  protected readonly term = new FormControl('', {
    nonNullable: true,
    validators: [Validators.required, Validators.minLength(2)],
  });

  protected runSearch(event: Event): void {
    event.preventDefault(); // this form has no NgForm, so stop the browser's native submit/reload
    if (this.term.valid) this.search.search(this.term.value);
  }

  protected select(book: CatalogBook): void {
    this.saveError.set(null);
    this.fieldErrors.set({});
    this.selected.set(book);
  }

  protected save(fields: StoreFields): void {
    const book = this.selected();
    if (!book) return;

    this.saving.set(true);
    this.saveError.set(null);
    this.addBook.execute({ ...book, ...fields }).subscribe({
      next: (added) => {
        this.toast.add({ severity: 'success', summary: 'Book added', detail: added.title });
        this.visible.set(false);
      },
      error: (e: unknown) => {
        this.saving.set(false);
        const err = e instanceof AppError ? e : null;
        this.fieldErrors.set(err?.fieldErrors ?? {});
        this.saveError.set(err?.message ?? 'Could not save the book.');
      },
    });
  }
}
