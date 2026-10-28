import { ChangeDetectionStrategy, Component, inject, input, output, signal } from '@angular/core';
import { MessageService } from 'primeng/api';
import { Dialog } from 'primeng/dialog';
import { Message } from 'primeng/message';
import { UpdateBookUseCase } from '../application/book-use-cases';
import { AppError } from '../domain/app-error';
import { Book, StoreFields } from '../domain/models';
import { StoreFieldsForm } from './store-fields-form';

@Component({
  selector: 'app-edit-book-dialog',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Dialog, Message, StoreFieldsForm],
  template: `
    <p-dialog
      [header]="'Edit “' + book().title + '”'"
      [(visible)]="visible"
      [modal]="true"
      [draggable]="false"
      [style]="{ width: '34rem' }"
      [breakpoints]="{ '700px': '95vw' }"
      (onHide)="closed.emit()"
    >
      <p class="mt-0 mb-4 text-sm text-muted-color">
        Title, author and other catalog details are fixed; you can change the store's genre, price, stock and description.
      </p>
      @if (saveError(); as msg) {
        <p-message severity="error" styleClass="mb-4 w-full">{{ msg }}</p-message>
      }
      <app-store-fields-form
        submitLabel="Save changes"
        [initial]="book()"
        [busy]="saving()"
        [serverErrors]="fieldErrors()"
        (submitted)="save($event)"
        (cancelled)="visible.set(false)"
      />
    </p-dialog>
  `,
})
export class EditBookDialog {
  private readonly updateBook = inject(UpdateBookUseCase);
  private readonly toast = inject(MessageService);

  readonly book = input.required<Book>();
  readonly closed = output<void>();

  protected readonly visible = signal(true);
  protected readonly saving = signal(false);
  protected readonly saveError = signal<string | null>(null);
  protected readonly fieldErrors = signal<Record<string, string[]>>({});

  protected save(fields: StoreFields): void {
    this.saving.set(true);
    this.saveError.set(null);
    this.updateBook.execute(this.book().id, fields).subscribe({
      next: (updated) => {
        this.toast.add({ severity: 'success', summary: 'Book updated', detail: updated.title });
        this.visible.set(false);
      },
      error: (e: unknown) => {
        this.saving.set(false);
        const err = e instanceof AppError ? e : null;
        this.fieldErrors.set(err?.fieldErrors ?? {});
        this.saveError.set(err?.message ?? 'Could not save the changes.');
      },
    });
  }
}
