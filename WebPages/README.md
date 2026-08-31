# OSDC.Drilling.Rig.WebPages

Reusable Razor class library for the Rig web UI.

It contains the `RigMain`, `RigEdit`, `RigFeatures`, and `StatisticsMain` pages together with the editor components, API clients, and helper utilities they depend on.

## Package contents

- Rig catalog page whose rows open directly in the editor
- Single create/edit workflow for the complete rig model, including identification, operating envelope, marine/jack-up/station-keeping profiles, storage and equipment
- Unit-aware fields for engineering quantities
- Named feature-assignment editor and feature-category catalog page
- Automatically displayed rig photo gallery with JPEG, PNG, and WebP upload, descriptive metadata, primary-photo selection, and a manual refresh/retry option
- Unit-aware mud-pump liner performance table for adding and removing the supported liner sizes and their associated flow and pressure ratings
- Usage statistics page
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

The Rig editor retrieves Vertical Datum data and resolves display labels through `MslDepthReferenceUtils`. The page configuration therefore requires a Vertical Datum endpoint in addition to the existing Rig, Unit Conversion, Field, and Cluster services. This package uses `OSDC.DotnetLibraries.Drilling.WebAppUtils` 1.1.3.
