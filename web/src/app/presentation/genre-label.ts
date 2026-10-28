import { Genre } from '../domain/models';

const LABELS: Record<Genre, string> = {
  Fiction: 'Fiction',
  SciFi: 'Sci-Fi',
  Fantasy: 'Fantasy',
  Mystery: 'Mystery',
  Biography: 'Biography',
  NonFiction: 'Non-fiction',
  Children: "Children's",
  History: 'History',
};

export const genreLabel = (g: Genre): string => LABELS[g];
