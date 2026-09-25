# AGENTS.md

## Project Overview

This repository contains a web-based administration system for the Sociedad de Fomento de Sierra de los Padres, Mar del Plata, Argentina.

The system will manage:

* Members
* Monthly social fees
* Fee payments
* Member debt
* Automatic generation of monthly fee obligations
* Basic accounting
* Income and expenses
* Administrative operations
* A simple member portal

The application is intended for a small organization, so simplicity, maintainability and correctness are more important than unnecessary architectural complexity.

## Technology Stack

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

* Microsoft SQL Server

## Architecture

Use a layered architecture:

```text
API
  ↓
Application
  ↓
Domain
  ↓
Infrastructure
  ↓
SQL Server
```

### Domain

Contains:

* Entities
* Value objects when useful
* Domain rules
* Domain-related enums

The Domain layer must not depend on Infrastructure or API.

### Application

Contains:

* Use cases
* Application services
* DTOs
* Interfaces
* Validation related to application operations

### Infrastructure

Contains:

* Entity Framework Core
* DbContext
* Entity configurations
* Repository implementations
* Database access
* External infrastructure concerns

### API

Contains:

* Controllers
* HTTP configuration
* Dependency injection configuration
* Authentication/authorization configuration
* API-specific models and configuration

Do not put business logic directly inside controllers.

## General Development Rules

1. Prefer simple solutions over unnecessary abstractions.
2. Do not introduce microservices.
3. Do not introduce CQRS unless there is a concrete requirement.
4. Do not introduce event sourcing.
5. Do not add libraries without a clear reason.
6. Follow SOLID principles where they improve maintainability.
7. Keep controllers thin.
8. Keep business rules out of the frontend.
9. Business rules that affect data correctness must be enforced by the backend.
10. Database operations must be performed through the Infrastructure layer.
11. Use asynchronous APIs where appropriate.
12. Use dependency injection.
13. Use DTOs for API contracts instead of exposing EF entities directly.
14. Do not duplicate business rules unnecessarily between frontend and backend.
15. Keep naming consistent and descriptive.

## Database Rules

Use SQL Server with Entity Framework Core 10.

Database changes must be represented through EF Core migrations.

Never modify an existing migration that may already have been applied to a shared or production database. Create a new migration instead.

Historical financial information must not be silently modified when business configuration changes.

For example, changing the current monthly fee must not change the amount of previously generated or paid obligations.

## Financial Data Rules

Financial data requires particular care.

Amounts must use appropriate decimal types.

Do not use floating-point types for monetary values.

Dates related to monthly fees must be represented consistently and must not depend on the user's browser locale.

Never silently alter historical financial records because of a current configuration change.

## Business Rules Currently Defined

### Members

A member has at least:

* First name
* Last name
* Address
* Date of birth
* DNI
* Registration/activation information

The exact member lifecycle must be implemented according to the requirements documented in the project.

### Monthly Fees

All members normally have the same monthly social fee.

The fee amount can change over time.

A fee amount change applies from a specified effective month onward.

Changing the current fee must NOT modify historical monthly obligations.

Monthly obligations are generated according to the member's registration/activation date.

### Fee Status

A monthly fee can have the following conceptual states:

* Pendiente
* Pagada
* Vencida

Rules:

* From the first day of the month through the last day of the month, an unpaid obligation is `Pendiente`.
* Once the month ends, an unpaid obligation becomes `Vencida`.
* A paid obligation is `Pagada`.

### Payments

A payment can cover one or multiple monthly fee obligations.

Members may pay several months in advance.

Partial payment of an individual monthly fee is not allowed.

The administrator records which months are covered by a payment.

Current payment methods include:

* Débito automático
* Efectivo
* Transferencia

A fee payment must automatically generate the corresponding accounting income.

### Member Reactivation

When a previously inactive member is reactivated, the member is treated as being registered again from the new activation date.

The system must not automatically create obligations for the inactive period unless the business requirements explicitly specify otherwise.

### Member Portal

Members can access a simple portal using their DNI.

The portal should allow the member to see relevant information about their current outstanding amount/debt.

Authentication should remain simple unless a security requirement requires a more sophisticated mechanism.

### Administration

Administrators must be able to search for a member by DNI and view:

* Member information
* Fee status
* Paid fees
* Outstanding fees
* Debt

### Accounting

The system includes basic accounting.

At minimum it must support:

* Income
* Expenses

Fee payments automatically generate income.

Expenses are entered separately by administrators.

Accounting records should preserve historical information.

## Development Workflow

Before implementing a new feature:

1. Understand the existing architecture.
2. Inspect the relevant entities and business rules.
3. Identify whether the requirement affects Domain, Application, Infrastructure, API or Frontend.
4. Implement the smallest coherent change.
5. Add or update tests where appropriate.
6. Run the relevant tests/build.
7. Do not modify unrelated parts of the application.

Do not make assumptions about unspecified business rules.

If a requirement is ambiguous and the ambiguity can affect financial data, database design or existing behavior, stop and ask for clarification instead of inventing a rule.

## Testing

Business rules should have automated tests.

Prioritize tests for:

* Fee generation
* Fee status transitions
* Fee amount changes
* Payments covering multiple months
* Advance payments
* Member activation/reactivation
* Accounting entries
* Debt calculations

Tests should verify business behavior rather than implementation details.

## Git

Use clear and descriptive commit messages.

Do not commit:

* Passwords
* API keys
* Connection strings containing credentials
* Environment-specific secrets
* Local development databases
* Build artifacts

Use environment configuration for secrets and environment-specific settings.

## Important

The requirements in this file are the currently agreed business rules.

Do not invent additional business behavior merely because it seems convenient.

When a new requirement conflicts with an existing documented rule, explicitly identify the conflict before implementing it.
