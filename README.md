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
├── frontend/
├── backend/
├── database/
├── docs/
├── AGENTS.md
└── README.md
```

## Development

Detailed development rules and business constraints are documented in `AGENTS.md`.

The project should be developed incrementally.

Each feature should be implemented, tested and reviewed before moving to the next feature.

## Current Status

The project is in the initial development stage.

The repository is being prepared before implementation of the first functional modules.
