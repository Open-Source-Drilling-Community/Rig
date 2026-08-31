# OSDC Drilling Rig

The Rig repository hosts the rig master-data microservice and reusable client web application. It describes rig identity, classification, rated operating envelope, optional land/offshore profiles, installed drilling equipment, and extensible capabilities.

Rig records contain static equipment specifications, certified limits, and
structured descriptions of available instrumentation. Live measurements and
changing controller state are intentionally handled outside this master-data
service.

# Solution architecture

The solution is composed of:
- **Model**
  - defines the main classes and methods to run the microservice
  - *dependencies* = BaseModels
- **Service**
  - defines the proper microservice API
  - *dependencies* = Model
- **ModelSharedOut**
  - contains C# auto-generated classes for microservice clients dependencies
  - these dependencies are stored as json files (following the OpenAPI standard) and C# classes are generated on execution of the program
  - these dependencies include the OpenApi schema of the microservice itself as well as other dependencies that may be useful to run the microservice
  - *dependencies* = Rig.json + some external microservices (OpenApi schemas in json format)
- **ModelTest**
  - performs unit tests on the Model (in particular for base computations)
  - *dependencies* = Model
- **ServiceTest**
  - microservice client that performs unit tests on the microservice (by default, an instance of the microservice must be running on http port 8080 to run tests)
  - *dependencies* = ModelShared
- **WebApp**
  - microservice web app client that manages data associated with Rig and allow to interact with the microservice
  - *dependencies* = ModelShared
- **home** (auto-generated)
  - data are persisted in the microservice container using the Sqlite database located at *home/Rig.db*

# Security/Confidentiality

Data are persisted as clear text in a unique Sqlite database hosted in the docker container.
Neither authentication nor authorization have been implemented.
Would you like or need to protect your data, docker containers of the microservice and webapp are available on dockerhub, under the digiwells organization, at:

https://hub.docker.com/?namespace=digiwells

# Deployment

Microservice is available at:

https://dev.digiwells.no/Rig/api/Rig

https://app.digiwells.no/Rig/api/Rig

Web app is available at:

https://dev.digiwells.no/Rig/webapp/Rig

https://app.digiwells.no/Rig/webapp/Rig

The OpenApi schema of the microservice is available and testable at:

https://dev.digiwells.no/Rig/swagger (development server) 

https://app.digiwells.no/Rig/swagger (production server)

# Funding

The current work has been funded by the [Research Council of Norway](https://www.forskningsradet.no/) and [Industry partners](https://www.digiwells.no/about/board/) in the framework of the cent for research-based innovation [SFI Digiwells (2020-2028)](https://www.digiwells.no/) focused on Digitalization, Drilling Engineering and GeoSteering. 

# Contributors

**Eric Cayeux**, *NORCE Energy Modelling and Automation*

## Current implementation

- Core Rig identity, `IsFixedPlatform`, `ClusterID`, equipment objects, and every top-drive controller property and enum token are retained.
- The master-data lift adds structured identification, rig/environment/mobility classification, rated operating envelope, marine, jack-up, station-keeping and storage profiles.
- Physical and logical rig components can carry stable component UUIDs; equipment also carries asset, lifecycle, installation and certification metadata.
- Rig features provide seven immutable built-in capability catalogs plus user-created catalogs. Assignments are validated atomically for category/option integrity, exclusivity, deprecation and validity periods.
- Feature-category create generates server-owned UUIDs. Updates use optimistic concurrency; referenced definitions and built-ins cannot be deleted.
- The SQLite catalog migration is additive: adding `RigFeatureCategoryTable` does not rebuild or delete `RigTable`.
- One or more JPEG, PNG, or WebP photographs can be attached to a rig. Photo metadata and binary content are persisted separately from the Rig JSON, normal reads remain lightweight, and `includePhotos=true` opts into metadata only.
- A versioned batch backup/restore API and web page export all rigs or an ordered selection together with referenced Rig Feature definitions, Cluster UUID/name manifests, and complete photograph content. Restore reconnects Cluster references by exact UUID or one unique normalized-name match, maps or creates local feature definitions according to policy, and commits catalog changes, rigs, and photos in one SQLite transaction.
- Image bytes are retrieved from a dedicated REST content endpoint and are excluded from ordinary MCP rig reads. The explicit batch-backup operation is the sole MCP exception because a recoverable backup must contain the photographs. Uploads are limited to 10 MiB and checksums are recorded with SHA-256.
- Mud-pump liner performance is modeled as a table of liner inner diameter, displacement per stroke, maximum flow rate, and maximum discharge pressure. Previously stored single-liner scalar values are mapped into one table row when records are read.
- The Rig service exposes all non-statistics Rig and feature-category REST operations as underscore-only MCP tools, together with `ping`. Access-statistics endpoints are intentionally excluded.
- MCP is available over streamable HTTP at `/rig/api/mcp` and WebSocket at `/rig/api/mcp/ws`; external MCP-hub registration is optional and disabled by default.
- Rig MCP tools now provide detailed operational descriptions and an explicit schema for the complete nested Rig payload. The schema covers caller-generated UUIDs, replacement-update semantics, fixed-platform `ClusterID` references, mast and equipment trees, exact enum strings, and SI units for physical values. Access-statistics endpoints remain excluded.
- The WebApp and reusable WebPages now integrate Vertical Datum data for mean-sea-level depth references.
