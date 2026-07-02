<div align="center">

# Pharmacy Management System

**Cross-platform desktop application for managing the daily work of a local pharmacy.**

A university database-systems project combining a **C# / Avalonia UI** desktop client with a relational **PostgreSQL** database accessed through **ODBC**. The system covers medicines, customers, prescriptions, compounded medicines, inventory, deliveries, supplier orders, sales documents, reports, audit logs and database backups.

<br />

![C#](https://img.shields.io/badge/C%23-.NET-7C3AED?style=for-the-badge&logo=csharp&logoColor=white)
![Avalonia UI](https://img.shields.io/badge/Avalonia_UI-Desktop-8B5CF6?style=for-the-badge)
![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Database-22D3EE?style=for-the-badge&logo=postgresql&logoColor=111827)
![ODBC](https://img.shields.io/badge/ODBC-Local_connection-111827?style=for-the-badge)
![MVVM](https://img.shields.io/badge/MVVM-Architecture-A78BFA?style=for-the-badge)

<br />

<a href="#screenshots">Screenshots</a> |
<a href="#main-features">Features</a> |
<a href="#architecture">Architecture</a> |
<a href="#running-locally">Running Locally</a> |
<a href="#documentation">Documentation</a>

</div>

---

## Project Snapshot

<table>
  <tr>
    <td><strong>Context</strong><br />Group university project at Wroclaw University of Science and Technology</td>
    <td><strong>Client</strong><br />C# desktop application built with Avalonia UI and MVVM</td>
    <td><strong>Database</strong><br />PostgreSQL schema with migrations, constraints, views, triggers and roles</td>
    <td><strong>Operations</strong><br />Sales, inventory, reports, audit logs, CSV export and backups</td>
  </tr>
</table>

## Authors

- Robert Tworek
- Kacper Wajda
- Michal Gladkojc

---

## Screenshots

<table>
  <tr>
    <td width="50%">
      <h3>Login and database connection</h3>
      <p>The login screen allows the user to authenticate and configure or test the local PostgreSQL/ODBC connection.</p>
      <img src="docs/screenshots/login.png" alt="Login screen" />
    </td>
    <td width="50%">
      <h3>Role-based dashboard</h3>
      <p>The main dashboard exposes pharmacy modules through a simple desktop navigation view.</p>
      <img src="docs/screenshots/dashboard.png" alt="Role-based dashboard" />
    </td>
  </tr>
  <tr>
    <td width="50%">
      <h3>Customer records</h3>
      <p>The customer module supports browsing, filtering and maintaining customer data, including contact details and PESEL handling.</p>
      <img src="docs/screenshots/clients.png" alt="Customer module" />
    </td>
    <td width="50%">
      <h3>Sales workflow</h3>
      <p>The sales module handles medicine selection, stock batches, cart items and sales documents such as receipts and invoices.</p>
      <img src="docs/screenshots/sale.png" alt="Sales module" />
    </td>
  </tr>
  <tr>
    <td colspan="2">
      <h3>Reports and administration</h3>
      <p>The reporting area contains sales, inventory, alerts, audit logs, CSV export and backup/restore actions.</p>
      <img src="docs/screenshots/reports.png" alt="Reports module" />
    </td>
  </tr>
</table>

---

## What The Project Shows

This repository demonstrates practical work with a business-style desktop application, not only isolated CRUD screens.

<table>
  <tr>
    <td width="64"><strong>01</strong></td>
    <td><strong>Desktop UI</strong><br />Avalonia interface structured with the MVVM pattern.</td>
  </tr>
  <tr>
    <td><strong>02</strong></td>
    <td><strong>Relational database design</strong><br />PostgreSQL migrations, constraints, views, triggers and role separation.</td>
  </tr>
  <tr>
    <td><strong>03</strong></td>
    <td><strong>Repository-based data access</strong><br />ODBC repositories separate SQL operations from UI state and user actions.</td>
  </tr>
  <tr>
    <td><strong>04</strong></td>
    <td><strong>Operational workflows</strong><br />Audit logs, backups, CSV exports, smoke tests and packaging scripts.</td>
  </tr>
</table>

The application includes role-based access for a pharmacy manager and a pharmacist, input validation, user-readable error handling, protection of sensitive customer data, and package scripts for Windows x64 and macOS Apple Silicon.

---

## Main Features

| Area | Implemented Scope |
| --- | --- |
| Authentication | Demo accounts, role detection, optional connection test and saved local database settings |
| Customers | CRUD for customers, addresses and phone numbers, PESEL validation, masking and encrypted storage |
| Medicines | Products, producers, variants, dosage, form, EAN codes, suppliers and search/filtering |
| Inventory | Medicine batches, raw material batches, stock corrections, reserved stock, expiry dates and low-stock alerts |
| Prescriptions | Prescription records and links between customers, doctors, sold medicines and prescription documents |
| Compounding | Recipes for prepared medicines, ingredients, raw materials, batch usage and execution records |
| Sales | Receipts, invoices, cart workflow, batch selection and automatic stock reduction |
| Deliveries | Deliveries for medicines and raw materials, order links, stock increases and manager-level correction flow |
| Orders | Manual orders, generated replenishment proposals, statuses, archiving and mixed product/raw-material lines |
| Reports | Sales reports, inventory reports, alerts, audit log preview and CSV export |
| Administration | User accounts, active status, roles, password changes and database-level permissions |
| Backup | Manual database backup and controlled restore from the reports module |

---

## Technologies

<table>
  <tr>
    <td><strong>Application</strong></td>
    <td>C# / .NET <code>net10.0</code>, Avalonia UI, CommunityToolkit.Mvvm, Material.Icons.Avalonia</td>
  </tr>
  <tr>
    <td><strong>Database</strong></td>
    <td>PostgreSQL, SQL migrations, seed data, constraints, views, triggers and smoke tests</td>
  </tr>
  <tr>
    <td><strong>Connectivity</strong></td>
    <td>ODBC / <code>System.Data.Odbc</code></td>
  </tr>
  <tr>
    <td><strong>Tooling</strong></td>
    <td>Bash and PowerShell setup, run, publish and package scripts</td>
  </tr>
</table>

---

## Architecture

```mermaid
flowchart TD
    UI["Avalonia Views"] --> VM["ViewModels"]
    VM --> REP["Repositories"]
    REP --> ODBC["ODBC"]
    ODBC --> DB[("PostgreSQL")]
```

The application keeps the UI, view state and database access separated. Views define the desktop interface, ViewModels handle user actions and state, repositories execute SQL operations, and PostgreSQL stores the business data with constraints, triggers, roles and reporting views.

---

## Database Design

The database is organized into three logical schemas:

| Schema | Responsibility |
| --- | --- |
| `apteka` | medicines, customers, doctors, prescriptions, sales and compounded medicine records |
| `magazyn` | suppliers, deliveries, inventory batches, raw materials and orders |
| `uzytkownicy` | users, roles, permissions and audit logs |

The database layer includes:

- normalized relational tables with primary and foreign keys;
- `NOT NULL`, `UNIQUE` and `CHECK` constraints;
- indexes for frequently searched fields;
- views for reports and operational summaries;
- triggers for audit logging;
- separate PostgreSQL roles for manager and pharmacist workflows;
- migrations from the initial schema to the final extended version;
- demo seed data and a smoke regression test.

---

## Security And Data Protection

The application includes several mechanisms that are important for a pharmacy-like domain:

| Mechanism | Purpose |
| --- | --- |
| Password hashing | User passwords are stored as hashes instead of plain text |
| PESEL encryption | Customer PESEL values are encrypted before saving |
| PESEL hash | A separate hash is used for uniqueness checks |
| Sensitive value masking | Selected values can be masked in the UI depending on role and context |
| Role separation | Manager and pharmacist workflows are separated in the app and PostgreSQL |
| Audit logs | Key operations are written to database logs |
| Restore confirmation | Database restore requires explicit user confirmation |

For a real deployment, the demo passwords and demo sensitive-data key must be replaced before storing real data.

---

## Project Structure

```text
pharmacy-management-system/
|-- Models/              # domain models
|-- Repositories/        # ODBC/PostgreSQL data access
|-- Services/            # validation, hashing, backup and sensitive data protection
|-- ViewModels/          # UI state and application logic
|-- Views/               # Avalonia screens
|-- db/
|   |-- migrations/      # schema and permission migrations
|   |-- seeds/           # demo data
|   `-- tests/           # SQL smoke regression test
|-- docs/
|   |-- screenshots/     # selected application screenshots
|   `-- reference/       # Polish project documentation and final report
|-- scripts/             # setup, run, publish and package scripts
|-- test-results/        # final UI/package test report
|-- appsettings.json
|-- Apteka.csproj
`-- Apteka.sln
```

---

## Running Locally

### Requirements

- .NET SDK compatible with `net10.0`
- PostgreSQL
- PostgreSQL ODBC driver
- On Windows: `PostgreSQL Unicode` ODBC driver
- On macOS/Homebrew: `psqlodbcw.so` is usually available under `/opt/homebrew/lib/`

### Database Setup

The database scripts are stored in `db/`.

macOS:

```bash
./scripts/setup-db-macos.sh
```

Windows:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\setup-db-windows.ps1
```

Manual setup:

```bash
createdb Apteka
for migration in db/migrations/*.sql; do psql -d Apteka -f "$migration"; done
psql -d Apteka -f db/seeds/001_demo_data.sql
psql -d Apteka -v ON_ERROR_STOP=1 -f db/tests/001_smoke_regression.sql
```

Demo accounts:

| Role | Login | Password |
| --- | --- | --- |
| Manager | `kierownik` | `kierownik123` |
| Pharmacist | `farmaceuta` | `farmaceuta123` |

### Application Start

```bash
dotnet restore
AVALONIA_TELEMETRY_OPTOUT=1 dotnet run --project Apteka.csproj
```

On macOS, the helper script can also be used:

```bash
./scripts/run-macos.sh
```

The default `appsettings.json` uses:

```json
"Driver": "auto"
```

In `auto` mode the application tries to use the Homebrew macOS ODBC driver when available, otherwise it falls back to the `PostgreSQL Unicode` driver name commonly used on Windows.

---

## Packaging

The project contains scripts for creating self-contained desktop builds:

| Platform | Command |
| --- | --- |
| macOS Apple Silicon | `./scripts/package-macos-app.sh` |
| Windows x64 | `./scripts/package-windows-zip.sh` |

The final submitted version produced:

- Windows x64 ZIP package;
- macOS Apple Silicon `.app`;
- macOS `.dmg`;
- database setup scripts for both platforms;
- demo database seed and SQL smoke test.

Large generated packages are better published as GitHub Releases instead of being committed directly into the repository history.

---

## Tests And Final Checks

The final project package was checked with:

- `.NET` build;
- SQL smoke regression test from `db/tests/001_smoke_regression.sql`;
- demo seed validation;
- encrypted PESEL storage check;
- ViewModel/screen loading check;
- Windows and macOS package generation;
- final ZIP content verification.

The detailed Polish test report is available in [test-results/ui-and-package-test-report-pl.txt](test-results/ui-and-package-test-report-pl.txt).

---

## Documentation

Detailed documentation is available in Polish:

- [Functional description and run guide](docs/reference/functional-description-pl.pdf)
- [Final functionality and test report](docs/reference/final-report-pl.pdf)

---

## Current Limitations

- The application is not code-signed or notarized, so Windows or macOS may show a warning on first launch.
- PostgreSQL is external by design. The project uses a local relational database, not an embedded database file.
- The macOS `.dmg` is a distribution image, not a full installer.
- Automatic scheduled backups are not included yet; backups are created manually from the reports module.
