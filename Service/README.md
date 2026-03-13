# Rig Microservice

## Overview

`NORCE.Drilling.Rig.Service` is the ASP.NET Core microservice for storing,
retrieving, updating, and deleting `Rig` objects from the `Model` project.

The service exposes a JSON-based HTTP API under the path base:

`/Rig/api`

It also exposes an OpenAPI/Swagger UI backed by the merged rig schema.

## Purpose

The microservice separates:

- the full `Rig` payload, stored as JSON
- the light-weight summary information used for listing and filtering
- usage statistics for the API itself

This allows clients to retrieve either:

- full rig records
- light-weight `RigLight` projections
- usage statistics

without duplicating the domain model in multiple persistence formats.

## Architecture

The service is built around:

- `Program.cs`: application bootstrap, dependency injection, Swagger, routing
- `RigController`: CRUD and query API for rigs
- `RigUsageStatisticsController`: usage-statistics API
- `SqlConnectionManager`: SQLite database initialization and schema management
- `RigManager`: persistence and retrieval logic for rig data

## Database Model

The service uses a single SQLite database file:

- file name: `Rig.db`
- home directory: `../home/`

The database contains exactly one application table:

- `RigTable`

### RigTable columns

The columns are intentionally aligned with the `RigLight` projection plus one
payload column containing the serialized `Rig` object:

- `MetaInfo`
- `Name`
- `Description`
- `CreationDate`
- `LastModificationDate`
- `IsFixedPlatform`
- `ClusterID`
- `data`

`data` contains the JSON-serialized `Rig` instance.

The service does not store a separate physical `ID` column. Lookup uniqueness is
enforced through an index on `json_extract(MetaInfo, '$.ID')`.

## JSON Serialization

The service uses shared `System.Text.Json` settings from `JsonSettings.cs`.

Notable characteristics:

- enums are serialized as strings
- the same settings are applied to controller responses and database payloads
- `Rig` records are round-tripped through JSON for persistence

## API Surface

### Base path

All routes are served under:

`/Rig/api`

### Rig controller

Controller:

- `RigController`

Primary routes:

- `GET /Rig/api/Rig`
  Returns all rig IDs
- `GET /Rig/api/Rig/MetaInfo`
  Returns all `MetaInfo` summaries
- `GET /Rig/api/Rig/{id}`
  Returns the full `Rig` for a given ID
- `GET /Rig/api/Rig/LightData`
  Returns all `RigLight` projections
- `GET /Rig/api/Rig/HeavyData`
  Returns all full `Rig` payloads
- `POST /Rig/api/Rig`
  Adds a new `Rig`
- `PUT /Rig/api/Rig/{id}`
  Updates an existing `Rig`
- `DELETE /Rig/api/Rig/{id}`
  Deletes a `Rig`

### Usage statistics controller

Controller:

- `RigUsageStatisticsController`

Route:

- `GET /Rig/api/RigUsageStatistics`
  Returns the in-memory usage statistics object

## Usage Statistics

Usage statistics are modeled by `UsageStatisticsRig` from the `Model` project.

The statistics controller exposes the current counters, while the rig controller
updates the relevant counters for:

- list operations
- meta-info requests
- light-data requests
- full-record requests
- create operations
- update operations
- delete operations

## Database Lifecycle

The database manager validates the on-disk schema against the expected single
table definition at startup.

If the schema does not match:

- the existing database is backed up with a timestamp
- the old schema is replaced
- the expected schema is recreated

## Swagger and OpenAPI

Swagger UI is configured from the merged OpenAPI document generated from the rig
schema.

Relevant endpoints:

- Swagger UI: `/Rig/api/swagger`
- merged JSON document: `/Rig/api/swagger/merged/swagger.json`

In debug builds, the project also includes a post-build target that regenerates
the schema artifact consumed by the service.

## Deployment Notes

The service is container-oriented and includes:

- `Dockerfile`
- Helm chart files under `charts/norcedrillingrigservice`

The historical project README referenced Docker Hub and hosted environments.
Those deployment references may still be valid operationally, but this README
focuses on the actual source-controlled service behavior.

## Current Technical Notes

- persistence is implemented with raw `Microsoft.Data.Sqlite`
- SQL commands in the rig manager use parameterized statements for stored rig
  content and metadata
- the service currently has two controllers matching the intended separation
  between rig operations and usage statistics
- local `dotnet build` may fail in the current workspace environment with exit
  code `1` and no diagnostics; this appears to be an environment or tooling
  issue rather than a documented source-level compiler error

## Source

The present microservice and web app solution was generated from a NORCE
Drilling and Wells Modelling team .NET template.

- creation date: `2025-09-03`
- template version: `4.0.9`
- template repository:
  `https://github.com/NORCE-DrillingAndWells/Templates`
- template documentation:
  `https://github.com/NORCE-DrillingAndWells/DrillingAndWells/wiki/.NET-Templates`

## Funding

The current work has been funded by the
[Research Council of Norway](https://www.forskningsradet.no/) and
[industry partners](https://www.digiwells.no/about/board/) in the framework of
[SFI Digiwells (2020-2028)](https://www.digiwells.no/).

## Contributors

- Eric Cayeux, NORCE Energy Modelling and Automation

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
