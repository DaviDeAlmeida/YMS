# YMS - Yard Management System

## Running

Requires the .NET 10 SDK and Docker.

```sh
docker compose up -d             # PostgreSQL 18 on localhost:5433 (user/password/database: yms)
dotnet test                      # HTTP-level tests (WebApplicationFactory) on a throwaway PostgreSQL (Testcontainers)
dotnet run --project src/YMS.Api # http://localhost:5230, Swagger UI at /swagger (document: /swagger/v1/swagger.json)
```

The host port is 5433 because a native PostgreSQL install on Windows usually holds 5432.

In Development every request is signed in as the seeded Owner from `Development:OwnerId` (`appsettings.Development.json`); authentication proper belongs to a later spec. The MediatR license key is read from `MediatR:LicenseKey` (user secrets or environment), never committed.

## Database

PostgreSQL through EF Core (Npgsql), snake_case names. Pending migrations are applied on start-up when `Database:MigrateOnStartup` is true (Development and tests); the connection string is `ConnectionStrings:Yms`.

```sh
dotnet tool restore
dotnet ef migrations add <Name> --project src/YMS.Infrastructure --output-dir Persistence/Migrations
dotnet ef database update --project src/YMS.Infrastructure
```

**Temporary infrastructure:** Batches go through an in-process queue until RabbitMQ comes in.
