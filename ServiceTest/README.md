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

The main test fixture is [`Tests.cs`](c:\NORCE-DrillingAndWells\Rig\ServiceTest\Tests.cs).

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

[`ServiceTest.csproj`](c:\NORCE-DrillingAndWells\Rig\ServiceTest\ServiceTest.csproj) references:

- [`Model`](c:\NORCE-DrillingAndWells\Rig\Model\Model.csproj)
- [`Service`](c:\NORCE-DrillingAndWells\Rig\Service\Service.csproj)

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
- Reuse the helper methods in [`Tests.cs`](c:\NORCE-DrillingAndWells\Rig\ServiceTest\Tests.cs) for object creation and `OkObjectResult` assertions when they fit.
- If test coverage expands beyond `RigController`, consider splitting the fixture into smaller controller-specific files to keep the suite maintainable.

## License

This project is provided under the MIT License. See [`LICENSE`](c:\NORCE-DrillingAndWells\Rig\ServiceTest\LICENSE).
