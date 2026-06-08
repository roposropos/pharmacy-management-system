# Pharmacy Management System

A university group project created at **Wrocław University of Science and Technology (Politechnika Wrocławska)** as part of a database course. The project is a desktop application for managing the daily operations of a local pharmacy.

The client application is written in **C#** with **Avalonia UI** and communicates with a **PostgreSQL** database through ODBC. The project combines a usable desktop interface with practical database concepts: product management, customers, prescriptions, sales, deliveries, orders, reports and role-based access.

> This is an academic portfolio project, not a production-ready medical system. The repository should be published only with the agreement of the project team.

## Authors

The project was developed as a group project by:

- Robert Tworek
- Kacper Wajda
- Michał Gładkojć

## Main features

- login flow and role-based navigation for a pharmacist and a pharmacy manager;
- management of products, manufacturers and drug variants;
- customer, address and phone-number management;
- prescriptions, sales and inventory updates;
- deliveries and wholesale orders;
- stock and sales reports with CSV export;
- input validation and user-friendly error messages;
- database-side audit concept for important operations.

## Technologies

- C# / .NET 10
- Avalonia UI
- PostgreSQL
- ODBC
- MVVM-style project structure

## Project structure

```text
pharmacy-management-system/
├── Models/             # domain models
├── Repositories/       # database access layer
├── Services/           # validation services
├── ViewModels/         # application logic and UI state
├── Views/              # Avalonia UI views and reusable components
├── database/           # placeholder for schema.sql and seed.sql
├── docs/
│   ├── screenshots/    # selected application screenshots
│   └── reference/      # project report and presentation in Polish
├── .env.example        # example database environment variables
├── Apteka.csproj
└── Apteka.sln
```

## Screenshots

### Role-based dashboard

![Role-based dashboard](docs/screenshots/role-based-dashboard.png)

### Product management

![Products module](docs/screenshots/products-module.png)

### Reports module

![Reports module](docs/screenshots/reports-module.png)

### Validation example

![Validation example](docs/screenshots/validation-example.png)

## Documentation

The repository includes the original Polish-language project materials:

- [Project report](docs/reference/project-report-pl.pdf)
- [Project presentation](docs/reference/project-presentation-pl.pdf)

The report describes the relational model, normalization, PostgreSQL schemas, constraints, views, indexes, roles and audit-log concept.

## Running the application

### Requirements

- .NET 10 SDK
- PostgreSQL
- PostgreSQL Unicode ODBC driver
- database schema and demo data matching the project model

The submitted source package did not contain the final SQL schema export or demo seed file. Add them under the `database/` directory before treating the repository as a complete reproducible setup.

Set the database connection variables in your shell:

```bash
export APTEKA_DB_DRIVER="PostgreSQL Unicode"
export APTEKA_DB_HOST="localhost"
export APTEKA_DB_PORT="5432"
export APTEKA_DB_NAME="Apteka"
export APTEKA_DB_USER="postgres"
export APTEKA_DB_PASSWORD="your-password"
```

Then run:

```bash
dotnet restore
dotnet run
```

## Security cleanup for the public version

The public portfolio version removes local connection details and avoids printing passwords or password hashes to the console. Database connection values are provided through environment variables.

## Academic context

The project was created as part of a group database project at Wrocław University of Science and Technology. It includes a relational database design with multiple schemas, constraints, reporting views, indexes, roles and an audit-log concept.
