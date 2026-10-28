import { ChangeDetectionStrategy, Component } from '@angular/core';

@Component({
  selector: 'app-footer',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <footer class="border-t border-surface py-4 text-center text-sm text-muted-color">
      Book Store · catalog data from
      <a href="https://openlibrary.org" target="_blank" rel="noopener" class="text-primary">Open Library</a>
    </footer>
  `,
})
export class Footer {}
