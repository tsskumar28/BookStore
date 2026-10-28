import { Provider } from '@angular/core';
import { BookRepository, CatalogRepository } from '../domain/repositories';
import { HttpBookRepository, HttpCatalogRepository } from './http-repositories';

/** Binds the domain ports to their HTTP adapters. Swap these to change the data source. */
export const provideInfrastructure = (): Provider[] => [
  { provide: BookRepository, useClass: HttpBookRepository },
  { provide: CatalogRepository, useClass: HttpCatalogRepository },
];
