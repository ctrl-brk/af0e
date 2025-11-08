# Project Context
Ham radio software projects

- **AF0E.UI**: Angular 21 (standalone, SCSS), **PrimeNG** UI.
- **Logbook.Api**: .NET 9 Minimal APIs + EF Core 9.
- **App**: Windows Forms and console applications
- **AF0E.DB**: SQL Server for production & local dev; SQLite (in-memory) for automated tests.
- **AF0E.Functions**: Azure functions, .NET 9
- **DX.Api**: .NET 9 Minimal APIs
- **IDE**: Rider (backend), WebStorm (frontend).

---

## API & Contracts
- Base path: `/api/v1/...`
- Use **TypedResults** with union return types (e.g., `Results<Ok<T>, NotFound>`). Keep endpoint methods thin.
- Shared DTOs and other objects live in `AF0E.Shared`
- Validation via FluentValidation (DTOs) and guard clauses in services.
- Lists support `page`, `pageSize`, `search`, `sort`.

# Angular Guidelines
- Use **HttpClient**; return strongly-typed interfaces.
- Standalone components, `OnPush`, light RxJS/signals, no global state until needed.
- Provide an HTTP interceptor for `baseUrl` and auth headers.

# EF Core & Persistence
- Disable lazy loading; prefer explicit `.Include()` or separate queries.
- SQL Server provider in prod/dev; SQLite provider only in tests.
- Enforce indexes & unique constraints.
- Keep migrations deterministic; avoid data access in migrations except seed data that’s truly static.

# Coding Standards
- **C#**: nullable enabled; `readonly` where possible; records for DTOs; use `Results`/`IResult` helpers; avoid static state; keep methods short.
- **TS**: `strict: true`; no `any`; small, cohesive files; prefer interfaces/zod for DTO shapes.
- Favor self-explanatory code over comments; add doc comments when intent isn’t obvious.
- Tests: xUnit for .NET, Vitest for Angular.

# What to Prefer
- Simplicity over patterns
- DI over statics; composition over inheritance.
- Small PRs with tests for non-trivial logic; integration tests for endpoints.

# What NOT to Do
- Don’t invent endpoints or tables not specified here.
- Don’t hardcode secrets/connection strings.
- Don’t add heavy dependencies or global state unless asked.
