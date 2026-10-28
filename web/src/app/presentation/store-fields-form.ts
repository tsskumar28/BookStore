import { ChangeDetectionStrategy, Component, OnInit, effect, input, output } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { Button } from 'primeng/button';
import { InputNumber } from 'primeng/inputnumber';
import { Select } from 'primeng/select';
import { Textarea } from 'primeng/textarea';
import { GENRES, Genre, StoreFields } from '../domain/models';
import { genreLabel } from './genre-label';

/** The store-owned fields (genre, price, stock, description), shared by "add" step 2 and "edit". */
@Component({
  selector: 'app-store-fields-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, Select, InputNumber, Textarea, Button],
  template: `
    <form [formGroup]="form" (ngSubmit)="submit()" class="flex flex-col gap-4" novalidate>
      <div class="flex flex-col gap-1">
        <label for="genre" class="font-medium">Genre</label>
        <p-select
          inputId="genre"
          formControlName="genre"
          [options]="genreOptions"
          optionLabel="label"
          optionValue="value"
          placeholder="Select a genre"
          appendTo="body"
          styleClass="w-full"
        />
        @if (error('genre'); as e) {
          <small class="text-red-500" role="alert">{{ e }}</small>
        }
      </div>

      <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div class="flex flex-col gap-1">
          <label for="price" class="font-medium">Price</label>
          <p-inputnumber
            inputId="price"
            formControlName="price"
            mode="currency"
            currency="USD"
            locale="en-US"
            [min]="0"
            styleClass="w-full"
            inputStyleClass="w-full"
          />
          @if (error('price'); as e) {
            <small class="text-red-500" role="alert">{{ e }}</small>
          }
        </div>
        <div class="flex flex-col gap-1">
          <label for="stock" class="font-medium">Stock <span class="text-muted-color">(optional)</span></label>
          <p-inputnumber
            inputId="stock"
            formControlName="stock"
            [min]="0"
            [useGrouping]="false"
            styleClass="w-full"
            inputStyleClass="w-full"
          />
          @if (error('stock'); as e) {
            <small class="text-red-500" role="alert">{{ e }}</small>
          }
        </div>
      </div>

      <div class="flex flex-col gap-1">
        <label for="description" class="font-medium">Description <span class="text-muted-color">(optional)</span></label>
        <textarea id="description" pTextarea formControlName="description" rows="3" class="w-full"></textarea>
        @if (error('description'); as e) {
          <small class="text-red-500" role="alert">{{ e }}</small>
        }
      </div>

      <div class="flex justify-end gap-2 pt-2">
        <p-button type="button" label="Cancel" severity="secondary" [disabled]="busy()" (onClick)="cancelled.emit()" />
        <p-button type="submit" [label]="submitLabel()" icon="pi pi-check" [loading]="busy()" />
      </div>
    </form>
  `,
})
export class StoreFieldsForm implements OnInit {
  readonly initial = input<Partial<StoreFields>>({});
  readonly submitLabel = input('Save');
  readonly busy = input(false);
  /** Field (camelCase) → messages returned by the API, shown inline. */
  readonly serverErrors = input<Record<string, string[]>>({});

  readonly submitted = output<StoreFields>();
  readonly cancelled = output<void>();

  protected readonly genreOptions = GENRES.map((g) => ({ label: genreLabel(g), value: g }));

  protected readonly form = new FormGroup({
    genre: new FormControl<Genre | null>(null, Validators.required),
    price: new FormControl<number | null>(null, [Validators.required, Validators.min(0.01), Validators.max(100000)]),
    stock: new FormControl<number | null>(0, [Validators.min(0)]),
    description: new FormControl('', { nonNullable: true, validators: [Validators.maxLength(4000)] }),
  });

  constructor() {
    effect(() => {
      for (const [field, messages] of Object.entries(this.serverErrors())) {
        const control = this.form.get(field);
        control?.setErrors({ server: messages[0] });
        control?.markAsTouched();
      }
    });
  }

  ngOnInit(): void {
    const i = this.initial();
    this.form.patchValue({
      genre: i.genre ?? null,
      price: i.price ?? null,
      stock: i.stock ?? 0,
      description: i.description ?? '',
    });
  }

  protected error(name: 'genre' | 'price' | 'stock' | 'description'): string | null {
    const c = this.form.controls[name];
    if (!c.errors || !(c.touched || c.dirty)) return null;
    if (c.errors['server']) return c.errors['server'] as string;
    if (c.errors['required']) return `${name === 'genre' ? 'Genre' : 'Price'} is required.`;
    if (c.errors['min']) return name === 'price' ? 'Price must be greater than 0.' : 'Cannot be negative.';
    if (c.errors['max']) return 'Price is too high.';
    if (c.errors['maxlength']) return 'Description is too long.';
    return 'Invalid value.';
  }

  protected submit(): void {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    const v = this.form.getRawValue();
    this.submitted.emit({
      genre: v.genre!,
      price: v.price!,
      stock: v.stock ?? 0,
      description: v.description.trim() || null,
    });
  }
}
