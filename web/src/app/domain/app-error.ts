export type AppErrorKind = 'validation' | 'conflict' | 'notFound' | 'unavailable' | 'network' | 'unknown';

/** Transport-neutral error: the infrastructure layer translates HTTP failures into this. */
export class AppError extends Error {
  constructor(
    readonly kind: AppErrorKind,
    message: string,
    /** Field name (camelCase) → messages, for validation errors. */
    readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(message);
    this.name = 'AppError';
  }
}
