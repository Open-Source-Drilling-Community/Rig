# Rig Microservice

## Overview

`OSDC.Drilling.Rig.Service` is the ASP.NET Core microservice for storing,
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
- `RigFeatureCategoryController`: immutable built-in and writable custom capability catalogs
- `RigPhotoController`: photo metadata, uploads, and on-demand binary content
- `RigUsageStatisticsController`: usage-statistics API
- `SqlConnectionManager`: SQLite database initialization and schema management
- `RigManager`: persistence and retrieval logic for rig data

## Database Model

The service uses a single SQLite database file:

- file name: `Rig.db`
- home directory: `../home/`

The database contains three managed application tables:

- `RigTable`
- `RigFeatureCategoryTable`
- `RigPhotoTable`

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

`RigFeatureCategoryTable` stores the feature-category metadata needed for compact discovery together with the complete serialized catalog. Seven built-in catalogs use stable UUIDs and are seeded idempotently. Custom categories are stored in the same table but remain distinguishable through `IsBuiltIn`.

`RigPhotoTable` keeps image bytes separate from the main Rig JSON. It stores descriptive metadata, display ordering, the primary-photo flag, media type, byte length, SHA-256 checksum, and the binary content. Deleting a rig also deletes its attached photos.

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
- `GET /Rig/api/Rig/{id}?includePhotos=false`
  Returns the full `Rig` for a given ID. Set `includePhotos=true` to add photo metadata; image bytes remain separate.
- `GET /Rig/api/Rig/LightData`
  Returns all `RigLight` projections
- `GET /Rig/api/Rig/HeavyData?includePhotos=false`
  Returns all full `Rig` payloads. Photo metadata is opt-in.
- `POST /Rig/api/Rig`
  Adds a new `Rig`
- `PUT /Rig/api/Rig/{id}`
  Updates an existing `Rig`
- `DELETE /Rig/api/Rig/{id}`
  Deletes a `Rig`

### Rig photo controller

- `GET /Rig/api/Rig/{rigId}/Photos`
  Returns photo metadata without image bytes
- `GET /Rig/api/Rig/{rigId}/Photos/{photoId}/Content`
  Streams the JPEG, PNG, or WebP content and supports range requests
- `POST /Rig/api/Rig/{rigId}/Photos`
  Uploads one multipart image plus descriptive metadata; the maximum size is 10 MiB
- `PUT /Rig/api/Rig/{rigId}/Photos/{photoId}?expectedModifiedUtc=...`
  Updates metadata using optimistic concurrency
- `DELETE /Rig/api/Rig/{rigId}/Photos/{photoId}`
  Deletes one photograph

Normal REST and MCP rig reads do not include photo metadata unless explicitly requested. MCP exposes optional metadata through `includePhotos`, but never returns binary image content; the media endpoints remain REST-only.

### Rig feature category controller

- `GET /Rig/api/RigFeatureCategory`
  Returns all category UUIDs
- `GET /Rig/api/RigFeatureCategory/MetaInfo`
  Returns compact metadata
- `GET /Rig/api/RigFeatureCategory/HeavyData`
  Returns all categories and options
- `GET /Rig/api/RigFeatureCategory/{id}`
  Returns one category
- `POST /Rig/api/RigFeatureCategory`
  Creates a custom category with server-generated category and option UUIDs
- `PUT /Rig/api/RigFeatureCategory/{id}?expectedModifiedUtc=...`
  Replaces a custom category with optimistic concurrency
- `DELETE /Rig/api/RigFeatureCategory/{id}`
  Deletes an unreferenced custom category

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

The database manager adds missing managed tables without changing existing
tables or data. If an existing managed table has an incompatible shape, a
timestamped database backup is created and only that incompatible table is
rebuilt. Unrelated tables are preserved.

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
- Helm chart files under `charts/osdcdrillingrigservice`

The historical project README referenced Docker Hub and hosted environments.
Those deployment references may still be valid operationally, but this README
focuses on the actual source-controlled service behavior.

## Current Technical Notes

- persistence is implemented with raw `Microsoft.Data.Sqlite`
- SQL commands in the rig manager use parameterized statements for stored rig
  content and metadata
- the service separates rig data, feature catalogs, photo media, and usage statistics into dedicated controllers
- mud-pump liner rows require a positive liner diameter, maximum flow rate, and maximum discharge pressure; duplicate liner sizes and pressures above the pump design pressure are rejected atomically

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

## MCP server

The service publishes the eight non-statistics Rig REST operations as MCP tools and deliberately omits access-statistics endpoints.

Descriptions distinguish compact discovery (`rig_get_all_ids`, `rig_get_all_meta_info`, and `rig_get_all_light`) from complete retrieval. The create and update tools expose an explicit schema generated from the service's Rig model, including every nested mast/equipment object, collection, enum value, rating, limit, and instrumentation capability. Live telemetry is outside the Rig master-data contract. This keeps the MCP contract synchronized as the Rig model evolves.

The schema documents caller-owned `MetaInfo.ID` values, the update path/body ID match, and the fixed-platform relationship: set `ClusterID` to an existing Cluster UUID when `IsFixedPlatform` is true and leave it null otherwise. Equipment objects are embedded full definitions, not separate resource references. Physical numbers use SI values (for example metres, pascals, kelvin, newtons, newton metres, watts, cubic metres per second, and radians). `DrillFloorElevation` is stored as a scalar in metres; because the payload has no vertical-datum field, callers must consistently apply their configured depth-reference convention.

- Streamable HTTP: `/rig/api/mcp`
- WebSocket: `/rig/api/mcp/ws`
- Utility tool: `ping`
- Optional external MCP-hub registration: configured in `appsettings.json`, disabled by default
