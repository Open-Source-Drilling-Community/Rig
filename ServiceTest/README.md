# ServiceTest

`ServiceTest` is the integration-style API test project for the Rig service.

It is a `.NET 8` NUnit test project that exercises the Rig API behavior directly against the service controller and persistence layer. The tests validate CRUD semantics, input validation, and the consistency of read models returned by the service after writes.

## Purpose

This project exists to verify that the Rig service behaves correctly at the API boundary without requiring manual checks through Swagger or the web application.

In particular, `ServiceTest` helps catch regressions in:

- request validation
- persistence behavior
- read/write consistency
- HTTP-style action results returned by the controller

The current suite focuses on the `RigController` endpoints and their interaction with the underlying SQLite-backed storage.

## What It Tests

The main test fixture is [`Tests.cs`](C:\OSDC\Rig\ServiceTest\Tests.cs).

It currently verifies behavior such as:

- empty database reads return empty collections
- invalid IDs and null payloads return `BadRequest`
- missing entities return `NotFound`
- duplicate creates return `409 Conflict`
- successful create operations persist data correctly
- update operations modify existing entities and refresh modification timestamps
- delete operations remove persisted entities

The tests call controller actions directly rather than sending HTTP requests over a running host.

## How It Works

Each test starts from a clean state:

1. The singleton `RigManager` instance is reset by reflection.
2. The SQLite database file is deleted and recreated as needed.
3. A new `SqlConnectionManager` is created.
4. A new `RigController` is constructed with fresh logging and storage dependencies.

This keeps the test suite isolated and deterministic across runs.

The database path is derived from `SqlConnectionManager.HOME_DIRECTORY` and `SqlConnectionManager.DATABASE_FILENAME`, so the tests exercise the same storage mechanism used by the service code.

## Project Dependencies

[`ServiceTest.csproj`](C:\OSDC\Rig\ServiceTest\ServiceTest.csproj) references:

- [`Model`](C:\OSDC\Rig\Model\Model.csproj)
- [`Service`](C:\OSDC\Rig\Service\Service.csproj)

It also uses:

- `Microsoft.NET.Test.Sdk`
- `NUnit`
- `NUnit3TestAdapter`
- `NUnit.Analyzers`

## Usage

Run the test project from the solution root with:

```powershell
dotnet test ServiceTest/ServiceTest.csproj
```

You can also run the full solution test suite with:

```powershell
dotnet test Rig.sln
```

## Runtime Characteristics

Important details for contributors:

- The tests are not pure unit tests; they validate controller and persistence behavior together.
- The suite does not require the Rig microservice to be started separately because it instantiates the controller in process.
- The suite writes a test database file under the configured `home` directory location used by `SqlConnectionManager`.
- Because the test setup deletes the database file, the project should be run only against disposable local test data.

## When To Update These Tests

Update or extend `ServiceTest` when:

- Rig controller routes gain new CRUD behavior
- validation rules change
- persistence semantics change
- returned DTO projections such as `RigLight` or `MetaInfo` change
- regressions are found in service-side read/write handling

If a new API capability is added to the service, this project should usually gain matching tests in the same change.

## Notes For Contributors

- Keep tests isolated. Each test should be able to run independently.
- Prefer verifying externally observable controller behavior rather than internal implementation details.
- Reuse the helper methods in [`Tests.cs`](C:\OSDC\Rig\ServiceTest\Tests.cs) for object creation and `OkObjectResult` assertions when they fit.
- If test coverage expands beyond `RigController`, consider splitting the fixture into smaller controller-specific files to keep the suite maintainable.

## License

This project is provided under the MIT License. See [`LICENSE`](C:\OSDC\Rig\ServiceTest\LICENSE).

## MCP coverage

- `McpToolRegistrationTests.cs` verifies the published Rig tool set and confirms that statistics operations are absent.
- The registration tests also guard the detailed descriptions, explicit nested Rig schema, platform-to-Cluster relationship, enum publication, SI-unit guidance, and update ID-matching rule.
- `McpServerHttpTests.cs` verifies MCP initialization, tool listing, and representative calls against the in-process service.

The HTTP fixture uses `WebApplicationFactory`; no external service or listening port is required.

Semantic contract tests also verify typed rig UUID lookup arguments, managed read/light projection semantics and resource identity metadata. These bindings support generic reference-origin lookup without altering serialized values.

## Bounded light discovery

Resource and catalog discovery returns light pages, with `offset` defaulting to 0 and `limit` defaulting to 50 (allowed range 1–200). Names and relevant resource-reference filters are evaluated on the server. Unbounded full-object `HeavyData` routes and tools, including filtered full-object lists, have been removed. Existing UUID and HTTP-metadata listings remain available; use named light pages for routine discovery.

REST routes and MCP tool names (relative to the service's existing API path base):

- `/Rig/LightData` / `rig_discover`
- `/RigFeatureCategory/LightData` / `rig_feature_category_discover`

Pages contain `Items`, `Offset`, `Limit`, `TotalCount`, and `NextOffset`; `NextOffset` is null on the final page. Offset pages are individually consistent reads, not a snapshot spanning requests: concurrent inserts/deletes can change later pages. Refresh discovery when the underlying collection changes. EarthGeodesy's existing search pages instead expose `Items[].Value`, `Offset`, `Limit`, and `TotalCount`.

Single-record reads, result/chunk reads, and deliberate backup/export/restore operations are preserved. Backups can still contain complete resources and should be treated as explicit bulk operations.

This is a breaking REST/MCP contract change: update generated clients, UIs, and external integrations together with the service release. Removed heavy-list names are not compatibility aliases. No persisted engineering shapes, database names, or storage volumes are changed. Historical usage counters are retained; bounded discovery has a separate additive `DiscoveryRequestsPerDay` history in resource services.


Contract checks cover removed heavy routes/tools, bounded pagination and strict invalid-argument rejection, and omission of engineering collections from discovery. Persistence tests exercise ordering, filters, totals, continuation, empty results, and retained selected reads. Run transport tests against isolated test storage.
