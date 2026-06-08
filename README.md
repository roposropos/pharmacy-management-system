# Pharmacy Management System

A desktop application for managing the daily operations of a local pharmacy.
The system was developed as a **group university project at Wrocław University of Science and Technology (Politechnika Wrocławska)**.

## Authors

* Robert Tworek
* Kacper Wajda
* Michał Gładkojć

## Overview

The application supports the most important processes involved in running a small pharmacy, including:

* management of medicines and pharmaceutical ingredients;
* customer and prescription records;
* sales and purchase documents;
* warehouse stock control;
* deliveries and supplier management;
* orders to pharmaceutical wholesalers;
* reporting and audit logs;
* user roles and access control.

The system is designed as a local desktop application connected to a PostgreSQL database.

## Technologies

* C#
* .NET
* Avalonia UI
* PostgreSQL
* ODBC
* MVVM-style application structure
* pgModeler
* pgAdmin

## Main Features

### Medicines and inventory

* management of medicines, variants and batches;
* tracking available and reserved quantities;
* expiry date monitoring;
* support for pharmaceutical ingredients used in compounded medicines.

### Customers and prescriptions

* customer records;
* doctor records;
* prescription processing;
* support for both ready-made and compounded medicines.

### Sales and deliveries

* sales document handling;
* warehouse stock updates after sales;
* delivery registration;
* supplier management;
* manual and automatic order preparation.

### Reporting

The system includes reporting views for:

* current medicine inventory;
* current pharmaceutical ingredient inventory;
* daily sales;
* warehouse status;
* ingredient consumption.

### Roles and Security

The application supports two user roles:

* **Pharmacist** — daily operations such as sales, prescription processing and warehouse updates;
* **Pharmacy manager** — extended access to reports, corrections, orders and administrative functions.

The database includes:

* role-based permissions;
* validation constraints;
* primary and foreign keys;
* `UNIQUE`, `NOT NULL` and `CHECK` constraints;
* indexes for frequently searched fields;
* operation logging through functions and triggers.

## Database Structure

The PostgreSQL database is divided into three logical schemas:

```text
apteka       - medicines, customers, doctors, prescriptions and sales
magazyn      - deliveries, suppliers, batches, ingredients and orders
uzytkownicy  - users, roles and operation logs
```

The project includes relational modelling, data normalization and mechanisms designed to maintain data integrity.

## Application Structure

```text
pharmacy-management-system/
├── Assets/
├── Models/
├── Repositories/
├── Services/
├── ViewModels/
├── Views/
├── database/
├── docs/
├── App.axaml
├── App.axaml.cs
├── Apteka.csproj
├── Apteka.sln
└── README.md
```

## Screenshots

Screenshots of the application interface and selected database diagrams are available in:

```text
docs/screenshots/
```

## Documentation

The full project report and presentation are available in:

```text
docs/reference/
```

The documentation contains a detailed description of:

* requirements;
* use cases;
* database diagrams;
* normalization;
* data integrity constraints;
* views, functions and triggers;
* security mechanisms;
* testing scenarios.

## Database Setup

The application requires a local PostgreSQL instance.

The final exported database schema and sample data should be added to:

```text
database/
├── schema.sql
└── seed.sql
```

## Academic Context

This repository contains an educational project developed as part of a database systems course at Wrocław University of Science and Technology.

The goal of the project was to design and implement a structured database-backed desktop application reflecting realistic business processes in a local pharmacy.
