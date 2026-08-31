# Rig Model

## Overview

`OSDC.Drilling.Rig.Model` contains the rig master-data model used by the `Rig`
solution. The model is centered around the `Rig` aggregate and describes
drilling equipment, mast configuration, mud-system equipment, MPD equipment,
BOP-related equipment, and supporting metadata used by the rest of the
application.

The current model is primarily aligned with the `rigEquipment` worksheet from
the contextual data catalog spreadsheet. Enum-backed classifications were
transcribed from the spreadsheet `EnumerationTable`. Where the pre-existing
model contained additional rig-control concepts not present in the spreadsheet,
those concepts were retained when they still carried domain value. The clearest
example is the top-drive controller configuration.

## Design Goals

The model has been streamlined with four main rules:

1. Spreadsheet naming has priority when two names describe the same concept.
2. Repeated metadata fields are centralized in shared base classes.
3. Equipment that exists as a detailed spreadsheet concept is modeled directly.
4. The model remains serialization-friendly: all entities expose public
   parameterless constructors and nullable properties where data may be absent.

## Core Aggregates

### Rig

`Rig` is the root aggregate for the project. It contains:

- Lifecycle metadata: `MetaInfo`, `CreationDate`, `LastModificationDate`
- Identification fields: `Name`, `Description`
- Mud circulation and treatment equipment
- Power-generation equipment
- Hoisting and rotary equipment through `MainRigMast` and `AuxiliaryRigMast`
- Pressure-control and well-control equipment
- MPD equipment and associated control devices
- Platform-level metadata such as `DrillFloorElevation`, `IsFixedPlatform`,
  and `ClusterID`
- Structured identity and provenance through `RigIdentification`
- Explicit `RigType`, `RigEnvironment`, and `RigMobilityType` classifications
- Certified or advertised limits through `RigOperatingEnvelope`
- Optional `MarineUnitProfile`, `JackUpProfile`, and `StationKeepingSystem`
- General storage capacities and extensible `RigFeatureAssignment` records
- Optional `RigPhotoMetadata` in read responses when explicitly requested; binary image content is kept outside the core aggregate


### RigMast

`RigMast` models the equipment mounted around a mast or derrick, including:

- Pipe-handling support: `CatWalk`, `PipeRack`
- Rotary and drive equipment: `RotaryTable`, `TopDrive`, `Kelly`
- Hoisting-system detail: `Derrick` and a nested `HoistingSystem` containing
  `Drawworks`, `CrownBlock`, `TravellingBlock`, and `DrillLine`
- Pipe and pressure-handling components: `StandPipe`, `StandPipeManifold`,
  `RotaryHose`, `ChokeManifold`, `RigChokeList`
- Additional intervention tools: `TorqueTurnSub`, `IronRoughneck`,
  `CasingTongs`, `CasingRunningTool`, `CasingDriveSystem`, `CoilDriveSystem`

`RigMast.HoistingSystem` is the authoritative container for the drawworks,
crown block, travelling block, and drill line. This matches the serialized
contract and the web editor.

## Shared Base Types

### RigComponentBase

`RigComponentBase` defines the common descriptive properties shared by most
model entities:

- `ID`
- `Name`
- `Description`

This base type is used for components that are named domain objects but do not
need manufacturer metadata, such as `RigMast`, `CatWalk`, `ReturnFlowLine`,
and other lightweight subcomponents.

### RigEquipmentBase

`RigEquipmentBase` extends `RigComponentBase` with common equipment identity
properties:

- `Manufacturer`
- `Model`
- `ProductCode`
- `SerialNumber`
- `AssetTag`
- `InstallationDate` and `CommissioningDate`
- `LifecycleStatus`
- `CertificationReferences`
- `MeasurementCapabilities`

This base type is used by the spreadsheet-derived equipment classes such as
`MudPump`, `TopDrive`, `Generator`, `MudTank`, `CementPump`,
`DrillingChokeManifold`, `BopStack`, `CementUnit`, and related types.

Centralizing these fields avoids drift and ensures a consistent public contract
for all equipment records.

`EquipmentMeasurementCapability` describes installed instrumentation without
storing live telemetry. It records a stable measurement code, an OSDC physical
quantity, sensor/calculation provenance, an optional source component, equipment
identity, SI range and accuracy, and nominal update frequency. Current pressures,
flows, temperatures, speeds, setpoints, and equipment states are intentionally
outside the Rig master-data contract.

## Main Equipment Groups

### Power and Drive Equipment

- `Generator`
- `DriveMode`
- `TopDrive`
- `Kelly`
- `CasingDriveSystem`
- `CoilDriveSystem`

These classes capture power-generation and rotation/drive configuration. The
`TopDrive` class also retains the control-tuning parameters that predated the
spreadsheet import:

- `TopDriveControllerType`
- `ProportionalGain`
- `IntegralGain`
- `TuningFrequency`
- `VFDFilterTimeConstant`
- `EncoderTimeConstant`
- `AccelerationFilterTimeConstant`
- `TorqueHighPassFilterTimeConstant`
- `TorqueLowPassFilterTimeConstant`
- `TuningFactor`
- `InertiaCorrectionFactor`

These properties were intentionally preserved even though they are not part of
the spreadsheet, because they remain valuable rig-control metadata.

The lift only adds commercial ratings (`RatedPower`, rated hoisting capacity,
continuous/intermittent torque, motor and automation compatibility). It does
not rename or remove any controller property or serialized controller value.

## Extensible Rig Features

`RigFeatureCategory`, `RigFeatureOption`, and `RigFeatureAssignment` provide
customizable classification without replacing engineering properties. The
predefined catalogs cover supported operations, advanced drilling, workflow,
automation, mobility/deployment, environmental suitability, and
energy/emissions. Built-ins use stable UUIDs and custom definitions use
server-generated UUIDs.

Features are intended for classification and discovery. Loads, pressures,
depths, dimensions, equipment, relationships, and controller parameters remain
strongly modeled properties.

### Mud System and Solids Control

- `MudPump`
- `MudTank`
- `ShaleShaker`
- `AuxSolidsControl`
- `FlowSensor`
- `MeasurementAfm`
- `ReturnFlowLine`
- `MudGasSeparator`
- `Desander`
- `Desilter`
- `Centrifuge`
- `Degasser`
- `CuttingsTransportSystem`
- `CuttingsDryer`
- `DrillingFluidTypeDescriptor`

Mud storage is represented by the ordered `MudTankList` collection.

`MudPump.Stroke` remains a pump-level mechanical dimension. Selectable fluid-end
configurations are represented by the ordered `LinerConfigurations` table. Each
`MudPumpLinerConfiguration` records liner inner diameter, displacement per
stroke, maximum volumetric flow rate, and maximum discharge pressure in SI
units. This captures the higher-flow/lower-pressure versus
lower-flow/higher-pressure trade-off between liner sizes.

### Cementing and Pumping

- `CementPump`
- `CementUnit`

`CementPump` includes the array-backed `CementPumpDisplacement` curve points
from the spreadsheet.

### Hoisting and Structural Equipment

- `Derrick`
- `Drawworks`
- `CrownBlock`
- `TravellingBlock`
- `DrillLine`
- `RiserHeaveCompensator`
- `DrillstringHeaveCompensator`

These classes model the detailed physical lifting and compensation equipment
that was previously hidden behind more generic placeholders.

### Pressure Control and Well Control

- `Accumulator`
- `BopStack`
- `FloatValve`
- `StandPipeManifold`
- `ChokeManifold`
- `DrillingChokeManifold`
- `FlowRoutingManifold`

`BopStack` includes array-backed lists describing stack components and BOP
lines. The manifold classes include curve/list helper records where the
spreadsheet defined structured array fields.

`BopStack.UnitReferences` and `CementUnit.Capabilities` are collections rather
than delimiter-dependent scalar strings. Choke, flow-meter, and pressure-sensor
voting counts are nullable integers.

### MPD and Separation Equipment

- `MpdController`
- `MpdControlDevice`
- `ContinuousCirculationDevice`
- `SurfaceMpdEquipment`
- `MarineMpdEquipment`
- `MultiPhaseSeparator`
- `DrillingMarineRiser`

These classes capture both the surface and subsea/rig-level equipment needed
for managed pressure drilling and related flow handling.

### Supplemental Rig-Floor Components

- `CatWalk`
- `PipeRack`
- `PipeDeck`
- `IronRoughneck`
- `CasingTongs`
- `CasingRunningTool`
- `StandPipe`
- `RotaryHose`
- `RotaryTable`
- `Slips`
- `RigChoke`

Several of these remain lightweight components with only shared descriptive
metadata because the current model does not yet require richer attributes for
them.

## Enumerations and Helper Records

Most categorical values live in `RigEquipmentTypes.cs`. This file contains:

- Equipment classifications such as `GeneratorClass`, `TopDriveClass`,
  `TankClass`, `BopStackClass`, `MarineMpdClass`
- Operating modes and controller classifications such as `DriveModeClass`,
  `ControlClass`, `MpdGradientMode`, `ControllerType`
- Fluid and drilling classifications such as `DrillingFluidClass` and
  `DrillingFluidType`
- Array helper records such as `ShakerScreenDefinition`,
  `CementPumpDisplacementPoint`, `ChokeCvCurvePoint`,
  `RoutingManifoldCurvePoint`, `BopStackComponentDefinition`,
  `BopLineDefinition`, and `EquipmentMeasurementCapability`

The intent is to keep most simple categorical primitives strongly typed rather
than using free-form strings.

## Serialization Characteristics

The model is designed for straightforward JSON serialization:

- Public setters are used throughout
- Parameterless constructors are preserved
- Nullable types represent optional or missing data
- Lists are used where the spreadsheet defines repeated structures

No custom converters are required for the core model classes.

## Naming and Streamlining Decisions

The following streamlining decisions are intentional:

- mud storage is represented by `MudTankList`
- detailed hoisting components are stored under `RigMast.HoistingSystem`
- spreadsheet-derived equipment classes inherit `RigEquipmentBase`
- lightweight named components inherit `RigComponentBase`
- `DrillingFluidType` remains the property name on `Rig` to align with the
  spreadsheet terminology, while the class name `DrillingFluidTypeDescriptor`
  avoids an awkward type/property name collision

## Modeling Boundary

- Numeric specifications and limits are stored in SI and rendered through the
  OSDC unit system.
- Live telemetry and changing controller state are not persisted with rig
  master data.
- DTO classes remain behavior-free; the service validates feature references,
  instrumentation ranges, catalog rules, and referential integrity at its API
  boundary.

## Project Contents

- `Rig.cs`: root aggregate
- `RigMast.cs`: mast/derrick sub-aggregate
- `RigComponentBase.cs`: shared component base classes
- `RigEquipmentTypes.cs`: enum and helper-record definitions
- individual `*.cs` files: equipment-specific classes
- `UsageStatisticsRig.cs`: usage and statistics-related support model

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE).
