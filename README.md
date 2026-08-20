# Library Management System — Clean Architecture

A Library Management backend built with **Clean Architecture**, **Domain-Driven Design**, and
**CQRS (MediatR)**, exposed through a **.NET Minimal API**. It supports managing books and
members, and letting members borrow and return books, tracking availability and borrowing
history.

The solution is split into four physically separate projects (`Library.Domain`,
`Library.Application`, `Library.Infrastructure`, `Library.Api`) with a one-way dependency rule
enforced by the compiler, not just a folder convention — see [Project structure](#project-structure)
below. Minimal APIs are the *presentation* mechanism (the outermost `Library.Api` layer), not the
architecture itself.

Built against the assessment brief in [`docs/dotnet-assessment-01.md`](docs/dotnet-assessment-01.md)
(not tracked in git — internal working notes; see [Assumptions](#assumptions--known-deviations) below).

## Technologies used

- .NET 10 / ASP.NET Core Minimal APIs
- Entity Framework Core 10 (Npgsql provider)
- PostgreSQL (via Docker Compose)
- MediatR (CQRS command/query dispatch)
- FluentValidation
- Swashbuckle (Swagger / OpenAPI)
- xUnit/MSTest + Testcontainers.PostgreSql (test projects scaffolded, see [Testing](#testing))

## Project structure

```
Library.Domain          Entities, value objects, domain errors, Result type (no external dependencies)
Library.Application     CQRS commands/queries + handlers, DTOs, validators, repository interfaces
Library.Infrastructure  EF Core DbContext, repository implementations, migrations
Library.Api             Minimal API endpoints, composition root (Program.cs), middleware
```

Each is a separate project with a one-way dependency rule (`Api → Application/Infrastructure →
Domain`), not just a folder convention — the compiler enforces the boundary.

## Running PostgreSQL with Docker

A `docker-compose.yml` is provided at the repo root:

```bash
docker compose up -d
```

This starts PostgreSQL 16 on `localhost:5432` with:

| Setting | Value |
|---|---|
| Database | `librarydb2` |
| Username | `postgres` |
| Password | `postgres` |

This matches the default connection string in `Library.Api/appsettings.json`
(`ConnectionStrings:LibraryDb`) — no changes needed for a local run.

## Running EF Core migrations

From the repository root, with the Postgres container running:

```bash
dotnet ef database update --project Library.Infrastructure --startup-project Library.Api
```

This applies the existing `InitialCreate` migration and creates the `Books`, `Members`, and
`Borrowings` tables (with a unique index on `Books.Isbn` and `Members.Email`).

To add a new migration after changing an entity:

```bash
dotnet ef migrations add <MigrationName> --project Library.Infrastructure --startup-project Library.Api
```

## Running the API

```bash
dotnet run --project Library.Api
```

By default (see `Library.Api/Properties/launchSettings.json`):

- HTTP: `http://localhost:5281`
- HTTPS: `https://localhost:7282`

## Accessing Swagger

With the API running in the `Development` environment (the default `dotnet run` profile),
Swagger UI is available at:

```
https://localhost:7282/swagger
```

It lists all endpoints grouped by tag (`Books`, `Members`, `Borrowings`) with request/response
schemas.

## Example API requests

```bash
# Create a book
curl -X POST https://localhost:7282/api/books \
  -H "Content-Type: application/json" \
  -d '{"title":"Clean Architecture","author":"Robert C. Martin","isbn":"9780134494166","publishedYear":2017,"totalCopies":3}'

# List all books
curl https://localhost:7282/api/books

# Get a book by id
curl https://localhost:7282/api/books/{id}

# Update a book
curl -X PUT https://localhost:7282/api/books/{id} \
  -H "Content-Type: application/json" \
  -d '{"title":"Clean Architecture","author":"Robert C. Martin","isbn":"9780134494166","publishedYear":2017,"totalCopies":5}'

# Delete a book
curl -X DELETE https://localhost:7282/api/books/{id}

# Register a member
curl -X POST https://localhost:7282/api/members \
  -H "Content-Type: application/json" \
  -d '{"name":"Jane Doe","email":"jane.doe@example.com","phoneNumber":"+1-555-0100"}'

# Borrow a book
curl -X POST https://localhost:7282/api/borrowings \
  -H "Content-Type: application/json" \
  -d '{"memberId":"{memberId}","bookId":"{bookId}"}'

# Get a member's borrowing history
curl https://localhost:7282/api/members/{memberId}/borrowings

# Return a book
curl -X POST https://localhost:7282/api/borrowings/{borrowingId}/return
```

Full endpoint list (all under `/api`):

| Resource | Method | Path |
|---|---|---|
| Books | `POST` `GET` | `/books` |
| Books | `GET` `PUT` `DELETE` | `/books/{id}` |
| Members | `POST` `GET` | `/members` |
| Members | `GET` `PUT` `DELETE` | `/members/{id}` |
| Members | `GET` | `/members/{memberId}/borrowings` |
| Borrowings | `POST` `GET` | `/borrowings` |
| Borrowings | `GET` | `/borrowings/{id}` |
| Borrowings | `POST` | `/borrowings/{id}/return` |

## Seed data

None currently — the database starts empty after migrations. Use the `POST` endpoints above to
create books and members before borrowing.

## Testing

`Library.Application.Tests` and `Library.Infrastructure.Tests` are scaffolded (the latter already
references `Testcontainers.PostgreSql` for integration testing against a real Postgres instance)
but contain no tests yet. Run them with:

```bash
dotnet test
```

## To be done

- **Field naming** differs slightly from the assessment brief in a couple of places:
  `Member.Name` (not `FullName`), `Borrowing.BorrowedAt`/`ReturnedAt` (not `BorrowedDate`/
  `ReturnedDate`). `Member.RegisteredDate` and `BorrowingStatus.Overdue` are not implemented —
  there's no overdue-detection logic in this version.
- **Error and validation response shapes** use ASP.NET Core's standard `ProblemDetails` /
  `ValidationProblemDetails` (RFC 7807) rather than a custom `{ statusCode, message, ... }`
  shape — chosen because it's the framework-idiomatic default and self-documents in Swagger,
  at the cost of not literally matching the brief's example JSON.
- **`GET /api/books` (list)** is still served by a small legacy service class rather than a
  MediatR query — a leftover from an in-progress CQRS migration, functionally identical to the
  CQRS-based endpoints.
- **Deleting a book or member with active borrowings is currently allowed.** There's no FK
  constraint between `Borrowings` and `Books`/`Members` in the database (by design — `Borrowing`
  references them by plain `Guid`, not an EF navigation property), so this won't fail, but it can
  leave a `Borrowing` record pointing at a deleted book/member. The brief doesn't specify a rule
  here, so no guard was added; flagging it as a real gap if referential integrity matters for your
  use case.
- **No soft delete, pagination, search, or audit fields** (`CreatedAt`/`UpdatedAt`/`DeletedAt`) —
  all listed as optional bonus challenges in the brief, not implemented.
- The `docker-compose.yml` `POSTGRES_DB` value was aligned to `librarydb2` to match
  `appsettings.json`'s connection string (they didn't match previously — a fresh `docker compose
  up` would have created a `librarydb` database that the app never connects to).
