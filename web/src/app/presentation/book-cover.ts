import { ChangeDetectionStrategy, Component, input, signal } from '@angular/core';

/** Cover thumbnail with a graceful placeholder when there is no URL or the image fails to load. */
@Component({
  selector: 'app-book-cover',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    @if (src() && !failed()) {
      <img
        [src]="src()"
        [alt]="'Cover of ' + title()"
        loading="lazy"
        class="h-16 w-11 rounded object-cover shadow-sm"
        (error)="failed.set(true)"
      />
    } @else {
      <div
        class="flex h-16 w-11 items-center justify-center rounded bg-surface-200 text-surface-500 dark:bg-surface-700"
        aria-hidden="true"
      >
        <i class="pi pi-book"></i>
      </div>
    }
  `,
})
export class BookCover {
  readonly src = input<string | null>(null);
  readonly title = input.required<string>();
  protected readonly failed = signal(false);
}
