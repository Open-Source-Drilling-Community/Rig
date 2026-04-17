# NORCE.Drilling.Rig.WebPages

Reusable Razor class library for the Rig web UI.

It contains the `RigMain`, `RigEdit`, and `StatisticsMain` pages together with the rig editor/viewer components, API clients, and helper utilities they depend on.

## Package contents

- Rig catalog page
- Rig editor page
- Usage statistics page
- Rig tree/editor/viewer components
- Host-configurable API access through injected configuration

## Dependencies

- `OSDC.DotnetLibraries.Drilling.WebAppUtils`
- `MudBlazor`
- `OSDC.UnitConversion.DrillingRazorMudComponents`
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
