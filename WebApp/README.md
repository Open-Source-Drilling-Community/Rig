# WebApp

`WebApp` is the server-side Blazor user interface for the Rig microservice.

It provides an interactive web client for browsing, creating, editing, and deleting Rig entities, as well as viewing simple usage statistics exposed by the service. The application is designed to sit in front of the Rig API and related supporting services such as Field, Cluster, and UnitConversion.

## Purpose

This project gives users a browser-based way to manage Rig data without working directly with raw API payloads or Swagger endpoints.

Its main responsibilities are:

- listing rigs stored by the Rig microservice
- showing rig details and read-only views of the full object graph
- editing complex Rig structures through specialized Razor components
- creating and deleting rigs
- loading related Field and Cluster data from external services
- presenting basic usage statistics from the Rig API

## Technology Stack

`WebApp` is built as an ASP.NET Core `.NET 8` web application using:

- Server-side Blazor
- Razor Pages
- MudBlazor for UI components
- Plotly.Blazor for chart support
- generated shared DTOs from `ModelSharedOut`
- domain model types from `Model`

The project file is [`WebApp.csproj`](c:\NORCE-DrillingAndWells\Rig\WebApp\WebApp.csproj).

## Application Structure

The startup entry point is [`Program.cs`](c:\NORCE-DrillingAndWells\Rig\WebApp\Program.cs).

At startup the application:

- reads optional service host URLs from configuration
- registers Razor Pages and Server-Side Blazor
- registers API client services for Rig, Field, and Cluster access
- configures MudBlazor services and snackbar behavior
- applies a path base of `/Rig/webapp`
- maps the Blazor hub and the fallback host page

The main user-facing pages are:

- [`Pages/RigMain.razor`](c:\NORCE-DrillingAndWells\Rig\WebApp\Pages\RigMain.razor)  
  Rig catalog/list page with search, refresh, detail viewing, edit navigation, and delete actions.
- [`Pages/RigEdit.razor`](c:\NORCE-DrillingAndWells\Rig\WebApp\Pages\RigEdit.razor)  
  Main editor for creating or updating rigs, including tree-based navigation of the nested Rig object graph.
- [`Pages/StatisticsMain.razor`](c:\NORCE-DrillingAndWells\Rig\WebApp\Pages\StatisticsMain.razor)  
  Summary view of usage statistics returned by the Rig service.

The component library under [`Components`](c:\NORCE-DrillingAndWells\Rig\WebApp\Components) contains reusable editors and viewers for the many nested rig-related model types.

## API Integration

The web app talks to several backend services:

- Rig microservice
- Field microservice
- Cluster microservice
- UnitConversion microservice

The client helpers are defined in:

- [`Shared/APIUtils.cs`](c:\NORCE-DrillingAndWells\Rig\WebApp\Shared\APIUtils.cs)
- [`Shared/RigApiClient.cs`](c:\NORCE-DrillingAndWells\Rig\WebApp\Shared\RigApiClient.cs)
- [`Shared/FieldClusterApiClient.cs`](c:\NORCE-DrillingAndWells\Rig\WebApp\Shared\FieldClusterApiClient.cs)

By default:

- Rig points to `https://localhost:5001/` with base path `Rig/api/`
- Field points to `https://dev.digiwells.no/` with base path `Field/api/`
- Cluster points to `https://dev.digiwells.no/` with base path `Cluster/api/`
- UnitConversion points to `https://dev.digiwells.no/` with base path `UnitConversion/api/`

These values can be overridden through configuration.

## Configuration

The configurable host URLs are surfaced through [`Configuration.cs`](c:\NORCE-DrillingAndWells\Rig\WebApp\Configuration.cs):

- `RigHostURL`
- `UnitConversionHostURL`
- `FieldHostURL`
- `ClusterHostURL`

These can be supplied through `appsettings`, environment variables, or other standard ASP.NET Core configuration sources.

Relevant configuration files include:

- [`appsettings.json`](c:\NORCE-DrillingAndWells\Rig\WebApp\appsettings.json)
- [`appsettings.Development.json`](c:\NORCE-DrillingAndWells\Rig\WebApp\appsettings.Development.json)
- [`appsettings.Production.json`](c:\NORCE-DrillingAndWells\Rig\WebApp\appsettings.Production.json)

## Usage

Run the app locally from the solution root with:

```powershell
dotnet run --project WebApp/WebApp.csproj
```

When running locally:

- the app hosts a server-side Blazor UI
- the Rig API is expected to be reachable at the configured host
- Field, Cluster, and UnitConversion endpoints must also be reachable if those views are used

Because the app applies `UsePathBase("/Rig/webapp")`, production-style hosting expects it to be served under that path base.

## Main User Flows

The current UI supports these primary flows:

- browse the rig catalog
- search rigs by name, description, or ID
- open a read-only detail panel for an existing rig
- navigate to create or edit screens
- edit complex nested rig structures with tree-driven navigation
- assign fields and clusters when relevant
- delete existing rigs
- review aggregate usage statistics

## Deployment Artifacts

This project includes container and Helm deployment assets:

- [`Dockerfile`](c:\NORCE-DrillingAndWells\Rig\WebApp\Dockerfile)
- [`charts/norcedrillingrigwebappclient`](c:\NORCE-DrillingAndWells\Rig\WebApp\charts\norcedrillingrigwebappclient)

The web app is packaged as the `norcedrillingrigwebappclient` container in the broader deployment setup.

## Notes For Contributors

- `WebApp` currently references `Model.dll` and `ModelSharedOut.dll` from built outputs rather than project references. A local build of those dependencies may be required before this project compiles cleanly.
- Many editing surfaces are implemented as specialized Razor components. Extend those components instead of pushing all editing logic into a single page.
- API calls are performed through typed helper classes; new endpoints should normally be added there before being consumed by pages.
- `APIUtils` currently disables TLS certificate validation in its `HttpClientHandler`. That may be convenient for development, but it is a security-sensitive behavior and should be reviewed carefully before broader deployment changes.

## License

This project is provided under the MIT License. See [`LICENSE`](c:\NORCE-DrillingAndWells\Rig\WebApp\LICENSE).
