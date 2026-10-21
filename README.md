# Company API + Angular frontend

## Prerequisites
| What | Version | Check | Needed for |
|---|---|---|---|
| .NET SDK | 10.0 or newer | `dotnet --version` | backend |
| Node.js + npm | `^24.15` or `>= 26` (Angular 22 requires it; with nvm: `nvm install 24` or `26`) | `node --version` | frontend |
| HTTPS development certificate | trusted | `dotnet dev-certs https --trust` | `https://localhost:7000` |
| Google Chrome | any recent | | `npm test` only |
| Free ports | 5000, 7000, 4200 | | API (http / https), frontend |

No database and no other service is needed: data lives in memory. Start the **backend first**, the frontend calls it at `http://localhost:5000`.
Without the HTTPS certificate `dotnet run` fails to start; use `dotnet run --launch-profile http` to serve only `http://localhost:5000`.
First time in the app: sign in with the default user (see "Users and security") or register a new one.

## Run
Backend (http://localhost:5000, https://localhost:7000):
```
cd backend/CompanyApi.Api
dotnet run
```
Frontend (http://localhost:4200, Angular 22, Node ^24.15 or >= 26):
```
cd frontend
npm install
npm start
```
Frontend tests: `npm test` (Karma, 69 specs; needs Chrome).
Tests: `dotnet test` from the repo root (78 tests; stop the API first if it is running, it locks the DLLs).
Swagger opens at http://localhost:5000/swagger when you start the API from Visual Studio; with `dotnet run` open it yourself.

## API
| Method | Route | Result |
|---|---|---|
| POST | `/api/auth/register` `{email, password}` | 201 + `{token, email, expiresAtUtc}`; 400 invalid input; 409 email taken |
| POST | `/api/auth/login` `{email, password}` | 200 + token; 401 on wrong credentials |
| POST | `/api/companies` `{name, websiteUrl}` | **All `/api/companies` routes require `Authorization: Bearer <token>`** (401 without). 201 + body, `Location` header; 400 on validation failure; 409 if the domain is already registered |
| GET | `/api/companies?name=&domain=&page=&pageSize=&sort=` | 200, paged envelope (empty `items` if no match); 400 on invalid paging |
| GET | `/api/companies/{id}` | 200, or 404 |
| GET | `/api/companies/search?q=&domain=&page=&pageSize=&sort=` | 200, paged envelope ordered by `relevance` desc by default; 400 if `q` missing or paging invalid |

Swagger UI (Development only): http://localhost:5000/swagger. Register or log in, click **Authorize** and paste the token to call POST /api/companies.

The `domain` filter matches the stored domain and the full URL (case-insensitive, partial), so `acme.com`, `www.acme` and `https://` all work.

Paging (both list endpoints): `page` (default 1), `pageSize` (default 10, max 100) and `sort` = `relevance` (default), `name` or `newest`.
The response is `{ "items": [...], "total": 42, "page": 1, "pageSize": 10 }`. For the plain list, `relevance` keeps creation order.
A page past the end returns empty `items` with the real `total`.

Errors are RFC 7807 `ProblemDetails`; the message is in `title`.

## Design
Four backend projects, dependencies point inward:
```
Api ──► Application ──► Domain
 └────► Infrastructure ──► Application
```
- `Domain`: the `Company` record.
- `Application`: DTOs, FluentValidation validators (`Validation/`, run by the services; all messages of a failed request are joined into the 400 `title`), `ICompanyService`/`CompanyService` (all rules), and the abstractions `IAppDbContext` (storage) and `IRelevanceScorer`.
- `Infrastructure`: `AppDbContext` (EF Core, in-memory provider today) with one configuration class per entity, and `DomainRelevanceScorer`.
- `Api`: controllers, error handler, and the composition root in `Program.cs` (`AddApplication()` + `AddInfrastructure()`).

Everything is wired with built-in dependency injection and constructor injection.
The internal `Company` record never leaves the service; the API only exposes DTOs. Domain errors
(`ValidationException`, `NotFoundException`) are mapped to 400/404 by one `IExceptionHandler`.
### Persistence
There is no repository layer: EF Core's `DbContext` already is the repository and the unit of work. Application only sees the small
`IAppDbContext` interface (`Companies`, `Users`, `SaveChangesAsync`), implemented by `AppDbContext` in Infrastructure.
Today it uses the EF Core **in-memory provider** (no database, data lives in the process and is lost on restart).

To change the data source, edit one place, `AddInfrastructure` in `Infrastructure/DependencyInjection.cs`: replace `UseInMemoryDatabase`
with `UseSqlServer` / `UseNpgsql` / `UseSqlite` plus a connection string, then add migrations.
Notes for that day:
- Filtering, ordering and paging of the plain list already run as queries in the data source (only the requested page is read).
- The relevance search scores candidates in C# (`DomainRelevanceScorer`), so it reads them in memory; use a normalized column or full-text search if the table gets big.
- Unique indexes on `Company.Domain` and `User.Email` are in the model. The in-memory provider ignores them, so the services also check explicitly; with a relational provider the index is what holds under concurrent requests (and the service turns that failure into a 409).
- The seed user tolerates an already seeded store, so restarts with persisted data are safe.

### Frontend structure
Standalone components with signals. Every component lives in its own folder, as `ng generate component` creates it
(`name.ts`, `name.html`, `name.css`, `name.spec.ts`), and HTTP lives in services.
```
src/app/
  app.*                         shell: header + login or companies page
  core/
    services/                   one folder per service, with its spec
      auth/                       AuthService (login, register, logout)
      companies/                  CompaniesService (list, get, create)
      session/                    SessionStore (token in localStorage, "expired" flag)
    auth.interceptor.ts         adds the token, expires the session on a 401
    models/                     one file per model/DTO (company, session, paged, row, sort, ...)
    api.ts, helpers.ts, http-errors.ts
  layout/header/
  shared/alert/                 success / error / empty messages
  auth/login/
  companies/
    companies-page/             layout: form next to directory
      company-form/
      company-directory/
        pagination/
```
Filtering, sorting and paging happen on the server; the components only hold the page being shown.

## Relevance strategy
Offline and explainable (`DomainRelevanceScorer`): the name is lowercased, accents/punctuation and legal
suffixes (inc, llc, corp...) are removed, then compared to the domain's main label
(`www.acme-tools.com` -> `acmetools`). Exact = 1.0, containment = 0.8, otherwise 0.8 x share of name words found at the start or end of the domain label.
Words shorter than 3 letters never count on their own (they match almost any label), and country domains are handled
(`cemex.com.mx` and `acme.co.uk` compare against `cemex` and `acme`).
A company is accepted when score >= 0.5. Search ranks by the best of domain score and name match (exact 1, prefix 0.9, substring 0.7).

Optional second chance (off by default): with `Relevance:InspectContent=true` (e.g. env var `Relevance__InspectContent=true`),
a name that fails the domain rule is also looked up in the page's `<title>`, meta description and `og:site_name`
(`HttpContentRelevanceChecker`; the share of the name's words found must be >= 0.5). It only runs when the domain rule fails.
Because the URL is user supplied it blocks connections to private/loopback addresses (SSRF), times out after 5 s and reads at most 256 KB.
Any failure scores 0, so the domain rule still decides. Limits: sites behind bot protection (e.g. abc.xyz) or rendered by JavaScript can not be read.

Duplicates: one company per domain (`www.` ignored); the service checks it, and a unique index on `Domain` enforces it in a relational provider.

## Users and security
Register/login issue a JWT (HS256, 60 min). Passwords are stored as PBKDF2-SHA256 hashes with a random salt (`IPasswordHasher`),
and login gives the same error, and takes the same time, for an unknown email and a wrong password (600,000 PBKDF2 iterations, stored in the hash). Companies record who created them (`createdBy`, from the token's `email` claim).
All company endpoints (read and write) require a token; only register and login are public. Users live in the same store as companies (in memory today), so they are lost on restart.

`/api/auth/*` is rate limited per client address (`RateLimit:AuthPerMinute`, default 10; 429 when exceeded). Emails are capped at 254 characters.
The app refuses to start if `Jwt:Key` is missing or shorter than 32 characters.

Default user (Development only, from `appsettings.Development.json`): `admin@example.com` / `Admin123!`.

shortcut: the JWT signing key is in `appsettings.json` for convenience; in production set `Jwt__Key` as an environment variable or secret.

## Not included (extensible)
Update/delete, roles, refresh tokens. Note a brand whose domain differs from its name (e.g. "Google" at `abc.xyz`) is rejected.
