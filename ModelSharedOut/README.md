# ModelSharedOut

`ModelSharedOut` is the outbound shared-model generator for the Rig microservice solution.

This project is a `.NET 8` console application that reads one or more OpenAPI/JSON schema documents from [`json-schemas`](C:\NORCE-DrillingAndWells\Rig\ModelSharedOut\json-schemas), merges them into a single OpenAPI bundle, normalizes schema references, and then generates a C# client and DTO model for downstream consumers of the Rig service.

The generated code is intended to be referenced by client-side projects in this solution, such as `ServiceTest` and `WebApp`, so they can use a single strongly typed API client and a consistent contract model.

## Purpose

The Rig solution uses a distributed shared-model approach:

- Each microservice owns its own public API contract.
- Dependent services and applications consume that contract through generated models instead of duplicating DTO definitions by hand.
- External model dependencies can be checked into source control as OpenAPI JSON files and treated as the source of truth for regeneration.

`ModelSharedOut` automates that process for the Rig service by:

- loading all `*.json` files in [`json-schemas`](C:\NORCE-DrillingAndWells\Rig\ModelSharedOut\json-schemas)
- merging their OpenAPI `paths` and `components.schemas`
- rewriting schema references so type names use short, collision-resistant identifiers
- exporting a merged OpenAPI document for Swagger/UI exposure
- generating a C# client and DTO set in the namespace `NORCE.Drilling.Rig.ModelShared`

## What It Produces

Running the generator creates or overwrites these main artifacts:

- [`RigMergedModel.cs`](C:\NORCE-DrillingAndWells\Rig\ModelSharedOut\RigMergedModel.cs)  
  Generated C# client and DTO classes used by consumers of the Rig microservice.
- [`RigMergedModel.json`](C:\NORCE-DrillingAndWells\Rig\Service\wwwroot\json-schema\RigMergedModel.json)  
  Merged OpenAPI document copied into the `Service` project so it can be exposed publicly.

The generated C# file includes:

- a `Client` type for calling the Rig API over `HttpClient`
- DTOs such as `Rig`, `Field`, `Cluster`, `MetaInfo`, usage-statistics types, and many related equipment/domain types
- generated exception types for API failures

## How It Works

The generator entry point is [`Program.cs`](C:\NORCE-DrillingAndWells\Rig\ModelSharedOut\Program.cs).

At runtime it:

1. Locates the solution root by walking upward until it finds a `*.sln` file.
2. Reads all local OpenAPI JSON files from `ModelSharedOut/json-schemas/`.
3. Merges them into a single `OpenApiDocument`.
4. Uses [`OpenApiSchemaReferenceUpdater.cs`](C:\NORCE-DrillingAndWells\Rig\ModelSharedOut\OpenApiSchemaReferenceUpdater.cs) to:
   - clone schemas safely
   - strip namespace-qualified schema names down to short names
   - update all `$ref` schema references in paths, request bodies, responses, and nested schema graphs
5. Serializes the merged OpenAPI document.
6. Applies a compatibility fix that rewrites `openapi: 3.0.4` to `3.0.3` for current Swagger UI tooling compatibility.
7. Uses `NSwag` to generate the C# client and DTO classes.

## Prerequisites

- .NET SDK 8.0
- Restored NuGet packages for the solution
- One or more valid OpenAPI JSON files in [`json-schemas`](C:\NORCE-DrillingAndWells\Rig\ModelSharedOut\json-schemas)

## Usage

From the solution root:

```powershell
dotnet run --project ModelSharedOut/ModelSharedOut.csproj
```

Behavior to expect:

- If generated files already exist, the tool prompts before overwriting them.
- If standard input is redirected, the overwrite confirmation defaults to `Y`.
- If the `json-schemas` folder is missing, generation fails with an error.

## Typical Workflow

1. Add or update dependency OpenAPI JSON files in [`json-schemas`](C:\NORCE-DrillingAndWells\Rig\ModelSharedOut\json-schemas).
2. Run the generator.
3. Review the regenerated [`RigMergedModel.cs`](C:\NORCE-DrillingAndWells\Rig\ModelSharedOut\RigMergedModel.cs) and merged JSON bundle.
4. Rebuild the solution and run any relevant tests or client applications that consume the shared model.

## Maintaining Input Schemas

The current default workflow is local and explicit:

- dependency schemas are stored in source control as JSON files
- regeneration uses those checked-in files

This is deliberate because it makes contract changes visible in commits and avoids silently pulling incompatible upstream schema changes during a normal build.

`Program.cs` also contains commented code for online dependency discovery. That path is not enabled by default.

## Notes For Contributors

- Treat [`RigMergedModel.cs`](C:\NORCE-DrillingAndWells\Rig\ModelSharedOut\RigMergedModel.cs) as generated code. Do not make manual edits that you expect to keep.
- Make changes in the input schemas or generator logic instead.
- Keep schema naming consistent to reduce collisions when type names are shortened during merge.
- If you change the generation strategy, verify both generated outputs:
  - the C# client model in `ModelSharedOut`
  - the merged OpenAPI JSON served by `Service`

## Package Dependencies

The generator relies primarily on:

- `Microsoft.OpenApi.Readers`
- `NSwag.CodeGeneration.CSharp`

These are declared in [`ModelSharedOut.csproj`](C:\NORCE-DrillingAndWells\Rig\ModelSharedOut\ModelSharedOut.csproj).

## License

This project is provided under the MIT License. See [`LICENSE`](C:\NORCE-DrillingAndWells\Rig\ModelSharedOut\LICENSE).
