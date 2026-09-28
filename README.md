# Sistema de Administración - Sociedad de Fomento de Sierra de los Padres

Sistema web para la administración de socios, cuotas sociales, pagos y contabilidad básica de la Sociedad de Fomento de Sierra de los Padres, Mar del Plata, Argentina.

## Objetivo

Centralizar la administración de:

* Socios
* Cuotas sociales
* Pagos
* Deudas
* Ingresos
* Egresos
* Información administrativa

El sistema tendrá dos áreas principales:

### Administración

Permitirá a los administradores:

* Gestionar socios.
* Consultar socios mediante DNI.
* Gestionar cuotas.
* Registrar pagos.
* Consultar cuotas pagadas, pendientes y vencidas.
* Consultar deudas.
* Registrar egresos.
* Consultar ingresos y egresos.
* Consultar información contable básica.

### Portal del socio

Permitirá a los socios identificarse mediante su DNI y consultar principalmente su situación de cuotas y deuda.

## Stack tecnológico

### Frontend

* Angular 22
* TypeScript
* Angular Material

### Backend

* .NET 10
* ASP.NET Core Web API
* C#
* Entity Framework Core 10

### Database

* SQL Server

## Architecture

The backend uses a layered architecture:

```text
┌─────────────────────────┐
│          API            │
├─────────────────────────┤
│      Application        │
├─────────────────────────┤
│         Domain          │
├─────────────────────────┤
│     Infrastructure      │
├─────────────────────────┤
│       SQL Server        │
└─────────────────────────┘
```

The frontend communicates with the backend through HTTP APIs.

## Main Functional Areas

### Members

Member information includes:

* First name
* Last name
* Address
* Date of birth
* DNI
* Registration/activation information

### Monthly Fees

The organization normally uses a single monthly social fee amount.

The amount can change over time.

Each fee amount has an effective month. Historical obligations must retain the amount that applied when the obligation was generated.

### Fee Status

Unpaid fees are:

* `Pendiente` during their corresponding month.
* `Vencida` after the month ends.

Paid fees are:

* `Pagada`

### Payments

A payment can cover one or several monthly obligations.

Advance payments are supported.

Partial payment of an individual monthly obligation is not supported.

Supported payment methods:

* Débito automático
* Efectivo
* Transferencia

### Accounting

Every fee payment automatically creates an income entry.

Administrators can also register expenses.

The accounting module provides a basic view of:

* Income
* Expenses

## Project Structure

```text
/
├── backend/
│   ├── SociedadFomento.slnx
│   └── src/
│       ├── SociedadFomento.Api/
│       ├── SociedadFomento.Application/
│       ├── SociedadFomento.Domain/
│       └── SociedadFomento.Infrastructure/
├── frontend/
│   └── src/app/
│       ├── core/
│       ├── shared/
│       └── features/
├── AGENTS.md
├── README.md
└── SPEC.md
```

## Development

Detailed development rules and business constraints are documented in `AGENTS.md` and `SPEC.md`.

### Prerequisites

* .NET SDK 10.0.300 or a compatible .NET 10 feature band.
* SQL Server LocalDB for the default Windows development connection.
* Node.js 22.22.3, 24.15.0 or a later supported version.
* npm 11.6.2 or a compatible version.

### Backend

```powershell
dotnet restore backend/SociedadFomento.slnx
dotnet build backend/SociedadFomento.slnx --no-restore
dotnet test backend/SociedadFomento.slnx --no-build
dotnet run --project backend/src/SociedadFomento.Api
```

The default connection uses `(localdb)\MSSQLLocalDB` and contains no credentials. Override it with the `ConnectionStrings__DefaultConnection` environment variable when using another SQL Server instance.

After applying migrations manually, the first administrator can be bootstrapped by setting both `AdminBootstrap__Email` and `AdminBootstrap__Password`. If both are absent, startup continues without creating an administrator. Never store their values in repository files. Angular development must proxy API requests through the same origin so secure cookies and the `X-XSRF-TOKEN` antiforgery header work without permissive CORS. Login is limited to five attempts per minute per client IP.

To manage migrations, install the matching stable EF Core tool and use Infrastructure as the migrations project:

```powershell
dotnet tool install --global dotnet-ef --version 10.0.12
dotnet ef database update --project backend/src/SociedadFomento.Infrastructure --startup-project backend/src/SociedadFomento.Infrastructure
```

### Frontend

```powershell
cd frontend
npm ci
npm start
```

Use `npm run build` for a production build and `npm test -- --watch=false` for a single test run.

The project should be developed incrementally. Each feature should be implemented, tested and reviewed before moving to the next feature.

## Current Status

The initial structure, SQL Server persistence model, administrative Members API and administrator authentication are ready. User interfaces and the remaining business modules have not been implemented yet.
