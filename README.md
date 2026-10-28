# Book Store

A small, production-shaped book store. **Open Library is the catalog you search to *find* books; the SQLite database is the store you *own*.** A user searches Open Library (proxied through our API), picks a real book, adds store details (genre, price, stock), and the app persists that book as its own record. Open Library is never queried again for it.

| Project | Stack | Folder |
|---|---|---|
| REST API | .NET 10, EF Core + SQLite, hand-rolled mediator, Serilog | [`api/`](api) |
| Web client | Angular 21, PrimeNG 21, Tailwind CSS 4 | [`web/`](web) |

## Run it

**Prerequisites:** .NET 10 SDK, Node.js 20.19+ or 22.12+ (npm 10+). Internet access is needed for catalog search (the seeded store works offline).

**1. API** (http://localhost:5080)

```bash
cd api
dotnet run --project src/BookStore.Api --launch-profile http
```

The SQLite file (`bookstore.db`) is created next to the API on first start and seeded with 12 sample books. Delete it to reset. No API keys or secrets are required.

**2. Web** (http://localhost:4200), in a second terminal

```bash
cd web
npm ci
npm start
```

`npm start` runs `ng serve` with a dev proxy ([`proxy.conf.json`](web/proxy.conf.json)) forwarding `/api` to `http://localhost:5080`, so there are no CORS or URL settings to change. If the API runs elsewhere, edit the proxy target.

**Tests**

```bash
cd api && dotnet test      # 20 tests: handlers, mediator, Open Library client, HTTP integration
cd web && npm test         # 18 tests (Vitest): store, search state, HTTP repositories
```

### Configuration (API)

All in [`appsettings.json`](api/src/BookStore.Api/appsettings.json); override with environment variables (e.g. `OpenLibrary__TimeoutSeconds=3`).

| Key | Default | Purpose |
|---|---|---|
| `ConnectionStrings:Default` | `Data Source=bookstore.db` | SQLite file |
| `Cors:Origins` | `["http://localhost:4200"]` | Allowed web origins |
| `OpenLibrary:UserAgent` | `BookStoreAssessment/1.0 (contact: dev@example.com)` | Sent to Open Library (they ask for a contact). Please set your own. |
| `OpenLibrary:TimeoutSeconds` / `ResultLimit` | `8` / `10` | Keep calls polite and bounded |

## API

| Method & route | Purpose | Open Library? |
|---|---|---|
| `GET /api/catalog/search?q=` | Search Open Library, return normalized, transient results | Yes (proxied) |
| `GET /api/books?genre=&page=&pageSize=&sortBy=&desc=` | List store books (paged, sorted, filtered server-side) | No |
| `GET /api/books/genres` | Every genre with its book count | No |
| `GET /api/books/{id}` | One book | No |
| `POST /api/books` | Persist a selected result + genre/price/stock | No |
| `PUT /api/books/{id}` | Update store-owned fields (genre, price, stock, description) | No |
| `DELETE /api/books/{id}` | Remove | No |

`sortBy` is one of `title`, `author`, `genre`, `price`, `stock`, `publishedYear` (case-insensitive). Errors are RFC 7807 problem details: `400` validation (with per-field `errors`), `404`, `409` duplicate ISBN / Open Library key, `502` Open Library unavailable or timed out.

### Architecture (API)

```
api/src
  BookStore.Domain          Book entity, Genre enum. No dependencies.
  BookStore.Application     Mediator, ports (IBookRepository, IBookCatalog), and
    Features/                 one folder per use case (vertical slices):
      Books/{GetBooks,GetBookById,GetGenres,CreateBook,UpdateBook,DeleteBook}
      Catalog/SearchCatalog     each = request + validator + handler in one file
  BookStore.Infrastructure  EF Core/SQLite repository, Open Library client, seeder
  BookStore.Api             Minimal-API endpoints, error mapping, correlation ID, Serilog
```

- **Clean Architecture + Vertical Slice.** Layers point inward (Api → Infrastructure → Application → Domain), while the Application layer is organized by *feature* rather than by technical type. A slice's command/query, validator and handler live together; only genuinely shared things (`BookDto`, store-field rules, ports) sit above them.
- **CQRS + mediator.** Commands and queries are separate request types; endpoints only call `IMediator.Send`. I wrote a ~60-line mediator instead of using MediatR (commercial license): it resolves one handler per request from DI and runs the request's `IValidator<T>`s first, so validation can't be skipped by an endpoint. Trade-off: no pipeline-behavior chain; add one if more cross-cutting concerns appear.
- **Open Library isolation.** `IBookCatalog` (Application) is implemented by `OpenLibraryClient` (Infrastructure), the only code that knows its URLs, field names, and failure modes. Timeouts, non-2xx responses and malformed JSON all become one `CatalogUnavailableException` → `502`. Search results are transient DTOs; only `POST /api/books` writes anything.
- **Duplicates** are prevented twice: an existence check for a friendly `409`, and unique indexes on `Isbn` and `OpenLibraryKey` as the source of truth (a race still yields `409`).
- **Hand-written SQL:** `BookRepository.GetGenreCountsAsync` ([`BookRepository.cs`](api/src/BookStore.Infrastructure/Persistence/BookRepository.cs)), books grouped by genre with a count, powers the Header filter via `GET /api/books/genres`.
- **Structured logging.** Serilog with message templates (no string concatenation). `CorrelationIdMiddleware` reads/creates `X-Correlation-ID`, echoes it in the response, and attaches it to every log line, including the outbound Open Library call.
- **Persistence choices.** `EnsureCreated` + a seeder instead of EF migrations: simplest thing that works for a single-file demo DB (a real deployment would add migrations). `Price` is stored as SQLite `REAL` because EF can't `ORDER BY` a decimal in SQLite; it is rounded to 2 dp on read and validated to 2 dp on write.
- Seed books use real ISBNs for covers; their Open Library work keys are illustrative provenance values.

## Web

```
web/src/app
  domain/           models, AppError, repository ports (BookRepository, CatalogRepository). No Angular/HTTP.
  application/      BookListStore (shared state), add/update/delete use cases, CatalogSearchService
  infrastructure/   HttpClient repositories, DTOs, mappers, HTTP-error mapping, DI wiring (the only layer that knows URLs)
  presentation/     Header, Body, Footer, add/edit dialogs, shared store-fields form
```

- **Layers and dependency direction:** presentation → application → domain; infrastructure → domain. Components never see `HttpClient`; they use use-cases and the store. Repository ports are abstract classes that double as DI tokens and are bound to their HTTP adapters in [`providers.ts`](web/src/app/infrastructure/providers.ts), so the data source is swappable and tests substitute fakes.
- **Header ↔ Body state sharing (siblings, no `@Input/@Output`).** The shell renders exactly `<app-header/> <app-body/> <app-footer/>`. The genre dropdown (Header) and the grid (Body) both inject a **root-scoped, signal-based store**, [`BookListStore`](web/src/app/application/book-list.store.ts): Header calls `setGenre`, Body calls `setTable` for paging/sorting and renders `items()`. Why this over alternatives: signals give one synchronous source of truth with fine-grained change detection and no subscription management in components; an RxJS `Subject` event bus would scatter state and invite leaks; NgRx would be heavy for one query object. Internally a `switchMap` on the query drops stale responses when the user clicks quickly. The trade-off is a global singleton: fine for this single list, but a second independent grid would need its own store.
- **Grid:** PrimeNG lazy `p-table`, with sorting, paging and the genre filter executed server-side by the API. Loading, empty ("no books in this genre", "store is empty") and error (with Retry) states are all handled.
- **Add flow:** two steps in one dialog: search (idle / loading / no-results / error-with-retry states) → pick a result → genre + price (+ optional stock/description). Client-side validation, plus server `400` field errors and `409` duplicates are surfaced inline.
- **UI stack:** PrimeNG for components (table, select, dialog, inputs, buttons, toast, confirm); Tailwind for layout and spacing. PrimeNG's styles sit in a CSS layer beneath Tailwind utilities so they coexist.

## Decisions and trade-offs

- **Server-side paging/sort/filter** rather than client-side: the API supports it, it scales, and it keeps the grid honest about loading states.
- **One hand-rolled mediator** rather than MediatR (licensing) or Wolverine (more framework than needed here).
- **Light theme only.** PrimeNG's dark mode is disabled (`darkModeSelector: '.app-dark'` never applied) to avoid half-themed output; Tailwind's `dark:` is wired to the same selector so enabling it later is a one-line change.
- Test emphasis went to behavior with real seams: handlers against real in-memory SQLite (so unique indexes/collation/raw SQL are exercised), the Open Library client against a stub `HttpMessageHandler` (normalization, timeout, 5xx, garbage), an end-to-end HTTP test (search is transient → select → persist → duplicate `409`), and the web store/search/HTTP layers. There are no component or browser (e2e) tests.

## What I'd do next

- EF migrations and a Dockerless but reproducible DB setup script.
- Debounced search-as-you-type and "load more" for catalog results (uses `numFound`).
- Optional description fetch from the Open Library Works endpoint at add-time.
- Component tests for the dialogs and a Playwright smoke test for search → add → filter → delete.
- Optimistic concurrency on `PUT` (row version) and a dark theme.
