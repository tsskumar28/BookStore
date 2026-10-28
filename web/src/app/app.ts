import { ChangeDetectionStrategy, Component } from '@angular/core';
import { Body } from './presentation/body';
import { Footer } from './presentation/footer';
import { Header } from './presentation/header';

/** The shell renders exactly three sibling components; none is nested inside another. */
@Component({
  selector: 'app-root',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Header, Body, Footer],
  template: `
    <div class="flex min-h-screen flex-col">
      <app-header />
      <app-body />
      <app-footer />
    </div>
  `,
})
export class App {}
