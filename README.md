# YMS - Yard Management System

## Running

Requires the .NET 10 SDK.

```sh
dotnet test                      # HTTP-level tests (WebApplicationFactory)
dotnet run --project src/YMS.Api # http://localhost:5230, OpenAPI at /openapi/v1.json
```

In Development every request is signed in as the seeded Owner from `Development:OwnerId` (`appsettings.Development.json`); authentication proper belongs to a later spec. The MediatR license key is read from `MediatR:LicenseKey` (user secrets or environment), never committed.

**Temporary infrastructure:** persistence uses the EF Core in-memory provider and Batches go through an in-process queue. PostgreSQL (Npgsql + migrations), RabbitMQ, Testcontainers and Docker Compose come in once Docker is available on the development machine.
