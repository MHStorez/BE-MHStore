# MHStore Backend

ASP.NET Core 8 API for MHStore. This folder is ready to live in its own Git repository and deploy independently from the frontend.

## Requirements

- .NET SDK 8
- PostgreSQL

## Project structure

- `MHStore.API/` - ASP.NET Core API entrypoint and controllers
- `MHStore.Services/` - business services
- `MHStore.Repositories/` - Entity Framework Core DbContext and migrations
- `MHStore.sln` - solution used by local builds and CI

## Local configuration

Local config files are ignored by Git. Start from the safe example file:

```bash
cp MHStore.API/appsettings.example.json MHStore.API/appsettings.json
```

Update `MHStore.API/appsettings.json` with your local database and secrets.

For production, prefer environment variables instead of committing config files. ASP.NET Core maps nested config with double underscores, for example:

```bash
ConnectionStrings__DefaultConnection="Host=...;Port=5432;Database=...;Username=...;Password=..."
JwtOptions__SecretKey="a-long-random-secret-at-least-32-chars"
JwtOptions__Issuer="MHStore"
JwtOptions__Audience="MHStore"
SePay__WebhookApiKey="..."
SePay__BankCode="..."
SePay__AccountNumber="..."
SePay__AccountName="MHStore"
```

## Local development

```bash
dotnet restore MHStore.sln
dotnet run --project MHStore.API/MHStore.API.csproj
```

By default the frontend dev server proxies `/api` to `http://localhost:5018`.

## Build

```bash
dotnet build MHStore.sln --configuration Release
```

## Docker

Build:

```bash
docker build -t mhstore-backend .
```

Run:

```bash
docker run --rm -p 8080:8080 \
  -e ConnectionStrings__DefaultConnection="Host=host.docker.internal;Port=5432;Database=mhstoredb;Username=postgres;Password=CHANGE_ME" \
  -e JwtOptions__SecretKey="a-long-random-secret-at-least-32-chars" \
  -e JwtOptions__Issuer="MHStore" \
  -e JwtOptions__Audience="MHStore" \
  mhstore-backend
```

Open Swagger in development builds at `/swagger`. Production images run with the default ASP.NET Core environment unless you set `ASPNETCORE_ENVIRONMENT`.

## Database migrations

`MHStore.API/Program.cs` currently runs EF Core migrations automatically on startup. Make sure the production database user has migration permissions, or change this behavior before deploying to a stricter environment.

## CI/CD

The GitHub Actions workflow in `.github/workflows/ci.yml` runs:

1. `dotnet restore MHStore.sln`
2. `dotnet build MHStore.sln --configuration Release --no-restore`

Add provider-specific deploy steps later after choosing the hosting platform.
