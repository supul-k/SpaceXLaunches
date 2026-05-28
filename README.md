# SpaceXLaunches API

ASP.NET Core Web API that fetches SpaceX launch data and caches it in MS SQL Server.

---

## Architecture

Clean Architecture with 4 layers:

- **Domain** — entities, interfaces, shared types. No dependencies.
- **Application** — use cases, DTOs, service logic. Depends on Domain.
- **Infrastructure** — SQL repository (raw ADO.NET), SpaceX HTTP client. Depends on Application.
- **API** — controllers, DI wiring, startup. Depends on Application and Infrastructure.

---

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- MS SQL Server (local install or Docker)

---

## Libraries Used

| Library | Reason |
|---------|--------|
| `Microsoft.Data.SqlClient` | Official MS SQL Server driver. Used directly with raw SQL — no ORM as required. |
| `System.Text.Json` | Built into .NET. Used to parse the SpaceX API JSON response — no extra packages needed. |

---

## Data Source

[SpaceX API v4](https://github.com/r-spacex/SpaceX-API) 

## Environment Variables

Connection string for SQL Server and SpaceX Url:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SpaceXLaunches;User Id=sa;Password=YourPassword123!;TrustServerCertificate=True;"
  },
  "SpaceX": {
    "BaseUrl": "https://api.spacexdata.com/v4"
  }
}
```