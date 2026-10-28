import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Select } from 'primeng/select';
import { BookListStore } from '../application/book-list.store';
import { GENRES, Genre } from '../domain/models';
import { genreLabel } from './genre-label';

/** Owns the genre filter. Talks to the Body only through the shared {@link BookListStore}. */
@Component({
  selector: 'app-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, Select],
  template: `
    <header class="border-b border-surface bg-surface-0 dark:bg-surface-900">
      <div class="mx-auto flex max-w-6xl flex-wrap items-center justify-between gap-4 px-4 py-3">
        <h1 class="m-0 flex items-center gap-2 text-xl font-bold">
          <i class="pi pi-book text-primary"></i> Book Store
        </h1>

        <div class="flex items-center gap-2">
          <label for="genre-filter" class="text-sm text-muted-color">Genre</label>
          <p-select
            inputId="genre-filter"
            [options]="options()"
            optionLabel="label"
            optionValue="value"
            [ngModel]="store.genre()"
            (ngModelChange)="store.setGenre($event)"
            styleClass="w-52"
            appendTo="body"
          />
        </div>
      </div>
    </header>
  `,
})
export class Header {
  protected readonly store = inject(BookListStore);

  protected readonly options = computed(() => {
    const counts = new Map(this.store.genres().map((g) => [g.genre, g.count]));
    const label = (g: Genre) => (counts.has(g) ? `${genreLabel(g)} (${counts.get(g)})` : genreLabel(g));
    return [{ label: 'All genres', value: null }, ...GENRES.map((g) => ({ label: label(g), value: g }))];
  });
}
