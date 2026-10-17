# OSDC.Drilling.Rig.WebPages

This release targets MudBlazor 9.9.0 and the matching OSDC shared web component packages.

Reusable Razor class library for the Rig web UI.

It contains the `RigMain`, `RigEdit`, `RigFeatures`, `RigBackupRestore`, and `StatisticsMain` pages together with the editor components, API clients, and helper utilities they depend on.

## Package contents

- Rig catalog page whose rows open directly in the editor
- Single create/edit workflow for the complete rig model, including identification, operating envelope, marine/jack-up/station-keeping profiles, storage and equipment
- `RigType`-discriminated fixed-platform editing with Gaussian drill-floor depth and a default 0.5 m standard uncertainty
- Unit-aware fields for engineering quantities
- Named feature-assignment editor and a feature-category catalog page using the common resource-service compact grid and actions; built-in definitions remain read-only while custom code, description, and deprecation metadata remain editable
- Automatically displayed rig photo gallery with JPEG, PNG, and WebP upload, descriptive metadata, primary-photo selection, and a manual refresh/retry option
- Unit-aware mud-pump liner performance table for adding and removing the supported liner sizes and their associated flow and pressure ratings
- Batch backup/restore page for all rigs or a selected set, including dependency summaries, photographs, local feature-catalog mapping policy, UUID-conflict policy, atomic server validation, and JSON download/upload
- Persistent usage-statistics page with summary totals and sortable endpoint activity
- Rig tree and editor components
- Host-configurable API access through injected configuration

## Dependencies

- `OSDC.DotnetLibraries.Drilling.WebAppUtils`
- `MudBlazor`
- `OSDC.UnitConversion.DrillingRazorMudComponents` 3.4.1 or later; the rig equipment model uses `PowerRateOfChangeDrilling` and `ChokeOpeningRateDrilling`
- `Plotly.Blazor`
- `ModelSharedOut`

## Host integration

The consuming app should:

1. Reference this package.
2. Provide an implementation of `IRigWebPagesConfiguration`.
3. Register that configuration, `IRigAPIUtils`, `RigApiClient`, and `FieldClusterApiClient` in DI.
4. Add the `WebPages` assembly to the Blazor router `AdditionalAssemblies`.

## Required configuration

- `RigHostURL`
- `UnitConversionHostURL`
- `FieldHostURL`
- `ClusterHostURL`
- `VerticalDatumHostURL`

## Mean-sea-level depth references

The Rig editor retrieves Vertical Datum data and resolves display labels through `MslDepthReferenceUtils`. The page configuration therefore requires a Vertical Datum endpoint in addition to the existing Rig, Unit Conversion, Field, and Cluster services. This package uses `OSDC.DotnetLibraries.Drilling.WebAppUtils` 1.1.5.

## Semantic quantities

The reflection-driven editors use the shared Rig provider registry backed by SemanticCatalogue 0.16.0. Explicit bindings take precedence for ambiguous legacy names: equipment `Weight` displays as mass, generator speed as angular velocity, compensator capacity as force, controller gains as dimensionless, and standpipe elevations as length relative to the drill floor. Display conversion never changes the canonical SI value submitted to the service.

## Bounded light discovery

Resource and catalog discovery returns light pages, with `offset` defaulting to 0 and `limit` defaulting to 50 (allowed range 1–200). Names and relevant resource-reference filters are evaluated on the server. Unbounded full-object `HeavyData` routes and tools, including filtered full-object lists, have been removed. Existing UUID and HTTP-metadata listings remain available; use named light pages for routine discovery.

REST routes and MCP tool names (relative to the service's existing API path base):

- `/Rig/LightData` / `rig_discover`
- `/RigFeatureCategory/LightData` / `rig_feature_category_discover`

Pages contain `Items`, `Offset`, `Limit`, `TotalCount`, and `NextOffset`; `NextOffset` is null on the final page. Offset pages are individually consistent reads, not a snapshot spanning requests: concurrent inserts/deletes can change later pages. Refresh discovery when the underlying collection changes. EarthGeodesy's existing search pages instead expose `Items[].Value`, `Offset`, `Limit`, and `TotalCount`.

Single-record reads, result/chunk reads, and deliberate backup/export/restore operations are preserved. Backups can still contain complete resources and should be treated as explicit bulk operations.

This is a breaking REST/MCP contract change: update generated clients, UIs, and external integrations together with the service release. Removed heavy-list names are not compatibility aliases. No persisted engineering shapes, database names, or storage volumes are changed. Historical usage counters are retained; bounded discovery has a separate additive `DiscoveryRequestsPerDay` history in resource services.


Tables/selectors use light discovery. Editors retrieve the selected complete resource; category options load only for chosen or already assigned definitions. Slot context uses paginated light locations, and rig-history context reads the selected wellbore. Backup selection uses discovery while the dedicated export/restore APIs retain full backup semantics.

## Package release 2.0.0

This release packages the current WebPages and generated contracts. Update consuming WebApps to `OSDC.Drilling.Rig.WebPages` version `2.0.0` after it is available on nuget.org. This is a major update for the bounded light discovery contracts; rebuild consumers together with their updated service clients.

## Trusted NuGet publishing

The `publish-webpages-nuget.yml` workflow authenticates with GitHub OIDC through `NuGet/login@v1`; no long-lived `NUGET_API_KEY` secret is used. Set `NUGET_USER` to the nuget.org profile username as a repository variable (a repository secret is also accepted). The publishing job requests `contents: read` and `id-token: write` and obtains its temporary credential immediately before uploading.

On nuget.org, the package owner must configure a trusted-publishing policy for `Open-Source-Drilling-Community/Rig`, workflow filename `publish-webpages-nuget.yml`, with permission to push new versions of `OSDC.Drilling.Rig.WebPages`. Leave the policy environment empty because the workflow does not declare a GitHub environment. Trigger the workflow manually with the intended version or push a `webpages-v*` tag. Already published versions are skipped.

## Package release 2.0.1

Requires OSDC.UnitConversion.DrillingRazorMudComponents 3.4.9 so embedded unit selectors consume bounded UnitSystem discovery pages. Rebuild consuming WebApps after upgrading this package.
