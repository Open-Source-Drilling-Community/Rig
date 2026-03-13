# Rig Model Tests

## Overview

`NORCE.Drilling.Rig.ModelTest` contains the NUnit-based test suite for the
`NORCE.Drilling.Rig.Model` project.

The purpose of this project is to verify that the rig data model remains
consistent, serialization-friendly, and aligned with the spreadsheet-driven
structure adopted in the `Model` project.

## Scope

The test suite focuses on structural and contract validation rather than rich
domain behavior, because the model is primarily composed of DTO-style classes.

The current suite verifies:

- presence of public parameterless constructors across concrete model classes
- reflection-based instantiation of concrete model classes
- JSON round-trip serialization for model classes and helper types
- readability and writability of public instance properties
- string-based JSON serialization for public enums
- correctness of key public contract names such as `AuxiliaryRigMast`
- preference for spreadsheet-driven names and structures such as `MudTankList`
  and detailed mast equipment
- inheritance from `RigComponentBase` and `RigEquipmentBase`
- preservation of top-drive controller and tuning properties
- basic behavioral checks for `History`, `CountPerDay`, `UsageStatisticsRig`,
  and `RigLight`

## Test Strategy

The suite intentionally combines:

- targeted contract tests for specific model decisions
- reflection-driven tests to cover the full public model surface with less
  maintenance overhead
- lightweight behavioral tests for the few model-support classes that contain
  executable logic

This approach provides broad protection while keeping the test code compact
enough to evolve with the model.

## Project Structure

- `Tests.cs`: main NUnit test suite for the current model
- `ModelTest.csproj`: test project definition and dependencies

## Dependencies

The project uses:

- `NUnit`
- `NUnit3TestAdapter`
- `Microsoft.NET.Test.Sdk`
- `coverlet.collector`

It references the sibling `Model` project directly.

## Current Environment Note

In the current workspace environment, `ModelTest` may fail to build or run with
an exit code of `1` without emitting compiler diagnostics. This appears to be
an environment or runner issue rather than a normal compilation error in the
test source. The `Model` project itself builds successfully.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
