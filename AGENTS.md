# AGENTS.md

## 1. Project Overview

This repository contains a web-based administration system for the Sociedad de Fomento de Sierra de los Padres, Mar del Plata, Argentina.

The functional requirements and business rules for the system are defined in:

```text
SPEC.md
```

**SPEC.md is the authoritative source for functional requirements and business rules.**

This file (`AGENTS.md`) defines technical development rules, architecture guidelines and instructions for working on the repository.

If there is a conflict between this file and `SPEC.md` regarding functional behavior, `SPEC.md` takes precedence.

If a requirement is ambiguous or missing from `SPEC.md`, do not invent important business behavior. Ask for clarification when the decision could affect financial data, database structure, security or existing behavior.

---

# 2. Technology Stack

## Frontend

* Angular 22
* TypeScript
* Angular Material

## Backend

* .NET 10
* ASP.NET Core Web API
* C#
* Entity Framework Core 10

## Database

* Microsoft SQL Server

Use stable releases only.

Do not use preview, nightly, experimental or release-candidate versions unless explicitly requested.

---

# 3. Architecture

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

The architecture should remain simple and appropriate for an MVP.

Do not introduce unnecessary architectural complexity.

## 3.1 Domain

Contains:

* Entities
* Value objects when useful
* Domain rules
* Domain enums
* Domain-specific behavior

The Domain layer must not depend on Infrastructure or API.

## 3.2 Application

Contains:

* Use cases
* Application services
* DTOs
* Interfaces
* Application-level validation
* Business workflows that coordinate domain operations

## 3.3 Infrastructure

Contains:

* Entity Framework Core
* DbContext
* Entity configurations
* Repository implementations where repositories are useful
* Database access
* External infrastructure concerns

## 3.4 API

Contains:

* Controllers
* HTTP configuration
* Dependency injection configuration
* Authentication and authorization configuration
* API-specific models and configuration

Controllers must remain thin.

Do not place business logic directly inside controllers.

---

# 4. Frontend Architecture

Keep the frontend organized by feature rather than by a large collection of unrelated technical folders.

Prefer a structure similar to:

```text
frontend/
└── src/
    └── app/
        ├── core/
        ├── shared/
        └── features/
            ├── authentication/
            ├── members/
            ├── fees/
            ├── payments/
            ├── accounting/
            ├── dashboard/
            └── member-portal/
```

The exact structure can evolve as the application grows, but avoid unnecessary complexity.

Business rules that affect data correctness must be enforced by the backend.

Frontend validation is useful for user experience but must not be considered the authoritative enforcement of business rules.

---

# 5. General Development Rules

1. Prefer simple solutions over unnecessary abstractions.
2. Do not introduce microservices.
3. Do not introduce CQRS unless there is a concrete requirement.
4. Do not introduce event sourcing.
5. Do not add libraries without a clear reason.
6. Follow SOLID principles where they improve maintainability.
7. Keep controllers thin.
8. Keep business rules out of controllers.
9. Keep business rules that affect data correctness in the backend/domain.
10. Use dependency injection.
11. Use asynchronous APIs where appropriate.
12. Use DTOs for API contracts instead of exposing EF entities directly.
13. Avoid unnecessary duplication.
14. Keep naming consistent and descriptive.
15. Implement the smallest coherent change necessary for each requirement.

Do not modify unrelated parts of the application while implementing a feature.

---

# 6. Business Requirements

All functional requirements are documented in:

```text
SPEC.md
```

Do not duplicate the complete business specification in this file.

Before implementing a feature:

1. Read the relevant section of `SPEC.md`.
2. Understand the business rule.
3. Identify the affected layers.
4. Implement the smallest coherent change.
5. Add or update tests.
6. Run the relevant tests and build.
7. Review the resulting behavior against `SPEC.md`.

---

# 7. Financial Data

Financial information is critical to the system.

Use appropriate decimal types for monetary amounts.

Never use floating-point types such as `float` or `double` for monetary values.

Prefer a database representation appropriate for currency, such as:

```text
decimal(18,2)
```

unless a different precision is explicitly required.

All financial calculations must be deterministic.

Do not silently round monetary values.

Any rounding behavior must be explicit and consistent.

---

# 8. Historical Data

Historical financial information must be preserved.

Once a financial record has been created, changing current configuration must not silently alter historical information.

Examples include:

* Previously generated monthly fee amounts.
* Paid fees.
* Payment records.
* Accounting entries.
* Cancelled payments.

When a fee amount changes, historical fee obligations must retain the amount that was applicable when they were generated.

---

# 9. Payments and Accounting

Payment and accounting operations must be treated as related business operations.

When a fee payment is registered:

1. The payment must be recorded.
2. The selected fee obligations must be associated with the payment.
3. The corresponding accounting income must be generated according to `SPEC.md`.

When a payment is cancelled:

1. The payment must remain in the database as historical information.
2. Its active effect must be removed.
3. The associated fee obligations must become available according to the rules in `SPEC.md`.
4. The associated accounting effect must be reversed or cancelled according to the specification.

These operations should be implemented transactionally.

If one required part of the operation fails, the system should not leave partially applied financial changes.

---

# 10. Database Rules

Use SQL Server with Entity Framework Core 10.

Database schema changes must be represented through EF Core migrations.

Never modify an existing migration that may already have been applied to a shared or production database.

Create a new migration instead.

Use appropriate database constraints and indexes.

Important business invariants should be enforced at the database level when practical.

Examples include:

* Unique DNI.
* Appropriate relationships.
* Required fields.
* Referential integrity.

Do not rely exclusively on frontend validation.

---

# 11. Entity Framework Core

Prefer explicit entity configuration using:

```text
IEntityTypeConfiguration<T>
```

when the configuration becomes non-trivial.

Avoid exposing `IQueryable` from repositories/services across architectural boundaries unless there is a clear reason.

Avoid loading unnecessary data.

Use projection for read-only queries where appropriate.

Use transactions for operations that modify multiple related financial records.

---

# 12. Dates and Time

Dates are important to the monthly fee rules.

Monthly fee periods must be represented consistently.

Do not rely on the browser's locale to determine financial periods.

Do not use the current date in business logic without making the behavior testable.

Prefer an abstraction for obtaining the current date/time when needed by business logic.

This allows tests to simulate:

* The beginning of a month.
* The end of a month.
* The beginning of the following month.

---

# 13. Authentication and Authorization

The system has two conceptual user roles:

* Administrator
* Member

The authorization rules are defined in `SPEC.md`.

The backend must enforce authorization.

Never rely only on hiding frontend routes or UI elements to protect data.

A member must never be able to request or access another member's information by manipulating an HTTP request.

Administrator-only operations must be protected at the API level.

Passwords must never be stored in plain text.

Secrets and credentials must not be committed to the repository.

---

# 14. Validation

Validate input at appropriate boundaries.

Examples:

* API request validation.
* Domain invariants.
* Database constraints.

Do not rely on a single validation layer for important business rules.

Financial operations require server-side validation.

Examples include:

* Payment amount matching selected obligations.
* Preventing partial fee payments.
* Preventing duplicate or conflicting payment assignments.
* Preventing invalid fee states.

The exact business rules must come from `SPEC.md`.

---

# 15. Testing

Business rules should have automated tests.

Prioritize tests for:

* Member registration.
* Member deactivation.
* Member reactivation.
* Fee generation.
* Fee amount changes.
* Fee status transitions.
* Future fees.
* Advance payments.
* Payment allocation.
* Prevention of partial payments.
* Payment cancellation.
* Debt calculation.
* Automatic accounting entries.
* Accounting reversal after payment cancellation.
* Member authorization.
* Administrator authorization.

Tests should verify behavior rather than implementation details.

Financial business rules should have deterministic tests using controlled dates.

---

# 16. API Design

Use RESTful HTTP endpoints where appropriate.

Use appropriate HTTP methods and status codes.

Do not expose database entities directly as API responses.

Use request/response DTOs.

Return clear validation errors.

Do not expose sensitive information.

API behavior must respect the authorization rules defined in `SPEC.md`.

---

# 17. Error Handling

Do not expose internal exceptions, stack traces or database details to clients.

Use centralized exception handling where appropriate.

Return useful, consistent API errors.

Expected business validation failures should be represented as appropriate client errors rather than unhandled exceptions.

---

# 18. Logging

Use structured application logging where useful.

Logs should help diagnose:

* Authentication failures.
* Unexpected application errors.
* Important administrative operations.
* Financial operation failures.

Do not log:

* Passwords.
* Authentication tokens.
* Secrets.
* Sensitive personal information unnecessarily.

---

# 19. Git

Use clear and descriptive commit messages.

Do not commit:

* Passwords.
* API keys.
* Connection strings containing credentials.
* Authentication secrets.
* Environment-specific secrets.
* Local development databases.
* Build artifacts.
* `node_modules`.
* Generated binaries.

Use environment configuration for secrets and environment-specific settings.

---

# 20. Development Workflow

Before implementing a feature:

1. Read the relevant section of `SPEC.md`.
2. Inspect the existing architecture.
3. Identify affected entities and business rules.
4. Determine which layers must change.
5. Implement the smallest coherent solution.
6. Add or update tests.
7. Run the relevant tests.
8. Run the relevant build.
9. Review the implementation against `SPEC.md`.
10. Avoid unrelated changes.

Do not rewrite existing code unnecessarily.

Do not refactor unrelated parts of the application merely because they could be improved.

---

# 21. Handling Ambiguity

Do not invent important business rules.

If a requirement is ambiguous, determine whether the ambiguity affects:

* Financial calculations.
* Historical data.
* Database structure.
* Authentication.
* Authorization.
* Member privacy.
* Existing functionality.

If it does, ask for clarification before implementing it.

For minor technical decisions that do not affect business behavior, choose the simplest maintainable solution and document the decision when appropriate.

---

# 22. MVP Scope

The application is an MVP.

Do not implement functionality outside the scope defined in `SPEC.md` unless explicitly requested.

Avoid:

* Microservices.
* Complex reporting systems.
* Unnecessary third-party integrations.
* Paid services when a suitable free alternative exists.
* Over-engineered abstractions.
* Features that are not required.

The goal is a simple, reliable and maintainable application.

---

# 23. Priority When Making Technical Decisions

When choosing between technically valid alternatives, prioritize:

1. Correctness.
2. Data integrity.
3. Security.
4. Simplicity.
5. Maintainability.
6. Testability.
7. Development speed.
8. Performance when it is relevant to the actual application.

Do not sacrifice financial correctness or security merely to reduce implementation effort.

---

# 24. Source of Truth

The repository uses the following documentation hierarchy:

### Functional requirements

```text
SPEC.md
```

### Technical development rules

```text
AGENTS.md
```

### General project documentation

```text
README.md
```

`SPEC.md` defines what the system must do.

`AGENTS.md` defines how the system should be developed.

`README.md` provides a general overview of the project.

When implementing functionality, always consult `SPEC.md` first.
