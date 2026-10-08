using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json.Nodes;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using Catalogue = OSDC.DotnetLibraries.Drilling.SemanticCatalogue.SemanticCatalogue;

#if RIG_WEBPAGES
namespace OSDC.Drilling.Rig.WebPages.Semantics;
#else
namespace OSDC.Drilling.Rig.Semantics;
#endif

/// <summary>
/// Provider-owned semantic bindings keyed by declaring model type and property.
/// REST, MCP and WebPages compile this source and therefore use one set of decisions.
/// </summary>
public static class ProviderSemantics
{
    public const string NestedBindingsExtension = "x-osdc-semantic-bindings";

    private sealed record Binding(string Concept, string? Role = null, string? Reference = null);

    private static readonly IReadOnlyDictionary<string, string> TypeConcepts =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Rig"] = Concepts.Rig,
            ["RigLight"] = Concepts.Rig,
            ["RigReadResponse"] = Concepts.Rig,
            ["MetaInfo"] = Concepts.ResourceMetadata,
            ["RigIdentification"] = Concepts.RigIdentification,
            ["RigComponentBase"] = Concepts.RigComponent,
            ["RigEquipmentBase"] = Concepts.RigEquipment,
            ["RigMast"] = Concepts.RigMast,
            ["HoistingSystem"] = Concepts.HoistingSystem,
            ["RigOperatingEnvelope"] = Concepts.RigOperatingEnvelope,
            ["MarineUnitProfile"] = Concepts.MarineUnitProfile,
            ["JackUpProfile"] = Concepts.JackUpProfile,
            ["StationKeepingSystem"] = Concepts.StationKeepingSystem,
            ["RigStorageCapacity"] = Concepts.RigStorageCapacity,
            ["EquipmentMeasurementCapability"] = Concepts.EquipmentMeasurementCapability,
            ["MudPumpLinerConfiguration"] = Concepts.MudPumpLinerConfiguration,
            ["RigType"] = Concepts.RigType,
            ["RigEnvironment"] = Concepts.RigOperatingEnvironment,
            ["RigMobilityType"] = Concepts.RigMobilityType,
            ["EquipmentLifecycleStatus"] = Concepts.EquipmentLifecycleStatus,
            ["MudPump"] = Concepts.MudPump,
            ["CementPump"] = Concepts.CementPump,
            ["MudTank"] = Concepts.MudTank,
            ["ShaleShaker"] = Concepts.ShaleShaker,
            ["Generator"] = Concepts.Generator,
            ["TopDrive"] = Concepts.TopDrive,
            ["RotaryTable"] = Concepts.RotaryTable,
            ["Drawworks"] = Concepts.Drawworks,
            ["BopStack"] = Concepts.BlowoutPreventerStack,
            ["MarineMpdEquipment"] = Concepts.ManagedPressureDrillingEquipment,
            ["SurfaceMpdEquipment"] = Concepts.ManagedPressureDrillingEquipment,
            ["DrillingMarineRiser"] = Concepts.DrillingMarineRiser,
            ["DrillstringHeaveCompensator"] = Concepts.HeaveCompensator,
            ["RiserHeaveCompensator"] = Concepts.HeaveCompensator,
            ["FlowRoutingManifold"] = Concepts.FlowRoutingManifold
        };

    private static readonly IReadOnlyDictionary<string, string> PropertyQuantities =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AutoDriller.MaxLimitRop"] = "RateOfPenetrationDrilling",
            ["AutoDriller.MinLimitRop"] = "RateOfPenetrationDrilling",
            ["AutoDriller.MaxLimitWob"] = "WeightOnBitDrilling",
            ["AutoDriller.MinLimitWob"] = "WeightOnBitDrilling",
            ["AutoDriller.MaxLimitTrq"] = "TorqueDrilling",
            ["AutoDriller.MinLimitTrq"] = "TorqueDrilling",
            ["BopLineDefinition.LineId"] = "DiameterPipeDrilling",
            ["BopLineDefinition.LineOd"] = "DiameterPipeDrilling",
            ["CasingDriveSystem.HoistingCapacity"] = "HookLoadDrilling",
            ["CasingDriveSystem.MaxLimitPushDown"] = "ForceDrilling",
            ["CoilDriveSystem.ReelPayloadCapacity"] = "ForceDrilling",
            ["CoilDriveSystem.ReelPayloadLength"] = "LengthStandard",
            ["CoilDriveSystem.InjectorHeadMinTubingOd"] = "DiameterPipeDrilling",
            ["CoilDriveSystem.InjHeadDesignPullCapacity"] = "ForceDrilling",
            ["CoilDriveSystem.InjHeadDesignSnubCapacity"] = "ForceDrilling",
            ["CoilDriveSystem.InjHeadPullCapacity"] = "ForceDrilling",
            ["CoilDriveSystem.InjHeadSnubCapacity"] = "ForceDrilling",
            ["CoilDriveSystem.InjHeadMaxSpeed"] = "AxialVelocityDrilling",
            ["ContinuousCirculationDevice.MaxLimitMudWeight"] = "MassDensityDrilling",
            ["ContinuousCirculationDevice.MaxLimitRotationRate"] = "AngularVelocityDrilling",
            ["CrownBlock.GrooveDiameter"] = "CableDiameterDrilling",
            ["CrownBlock.MaxLimitCompensatorStroke"] = "LengthStandard",
            ["Derrick.MaxLimitWindSpeed"] = "Velocity",
            ["DrillingChokeManifold.MaxLimitOpeningSpeed"] = "ChokeOpeningRateDrilling",
            ["DrillingChokeManifold.TrimSize"] = "DiameterPipeDrilling",
            ["DrillingChokeManifold.FlowMeterSize"] = "DiameterPipeDrilling",
            ["DrillingMarineRiser.JointWeight"] = "MassGradientPerLengthDrilling",
            ["DrillLine.Diameter"] = "CableDiameterDrilling",
            ["DrillLine.LinearWeight"] = "MassGradientPerLengthDrilling",
            ["DrillstringHeaveCompensator.CompensatorCapacity"] = "ForceDrilling",
            ["DrillstringHeaveCompensator.MaxLimitCompensatorStroke"] = "LengthStandard",
            ["FlowRoutingManifold.FlangeSize"] = "DiameterPipeDrilling",
            ["FlowRoutingManifold.PressureReliefValveTrim"] = "DiameterPipeDrilling",
            ["Generator.Speed"] = "AngularVelocityDrilling",
            ["Generator.PowerFactor"] = "ProportionStandard",
            ["Generator.StartupTimeCold"] = "DurationDrilling",
            ["Generator.StartupTimeWarm"] = "DurationDrilling",
            ["Generator.Voltage"] = "ElectricTension",
            ["Generator.MaxLimitVoltage"] = "ElectricTension",
            ["Generator.MinLimitVoltage"] = "ElectricTension",
            ["Generator.MaxLimitPowerIncrease"] = "PowerRateOfChangeDrilling",
            ["Generator.MaxLimitSpeedIncrease"] = "RotationalFrequencyRateOfChangeDrilling",
            ["Generator.MaxLimitFrequency"] = "Frequency",
            ["Generator.MinLimitFrequency"] = "Frequency",
            ["MarineMpdEquipment.Weight"] = "MassDrilling",
            ["MarineUnitProfile.HullLength"] = "LengthStandard",
            ["MarineUnitProfile.HullWidth"] = "LengthStandard",
            ["MarineUnitProfile.HullDepth"] = "LengthStandard",
            ["MarineUnitProfile.OperatingDraft"] = "LengthStandard",
            ["MarineUnitProfile.TransitDraft"] = "LengthStandard",
            ["MarineUnitProfile.OperatingDisplacement"] = "MassDrilling",
            ["MarineUnitProfile.VariableDeckLoad"] = "ForceDrilling",
            ["MarineUnitProfile.MaximumTransitSpeed"] = "Velocity",
            ["MeasurementAfm.UpdateRate"] = "Frequency",
            ["MpdControlDevice.NominalSize"] = "DiameterPipeDrilling",
            ["MpdController.PrimaryChokeTrim"] = "DiameterPipeDrilling",
            ["MpdController.SecondaryChokeTrim"] = "DiameterPipeDrilling",
            ["MudPump.MaxLimitOperatingSpeed"] = "StrokeFrequency",
            ["CementPumpDisplacementPoint.StrokeRate"] = "StrokeFrequency",
            ["RigOperatingEnvelope.MaximumDrillingDepth"] = "DepthDrilling",
            ["RigOperatingEnvelope.MaximumWaterDepth"] = "DepthDrilling",
            ["RigOperatingEnvelope.MaximumOperatingWindSpeed"] = "Velocity",
            ["RigOperatingEnvelope.MaximumSurvivalWindSpeed"] = "Velocity",
            ["RiserHeaveCompensator.CompensatorCapacity"] = "ForceDrilling",
            ["RiserHeaveCompensator.MaxLimitCompensatorStroke"] = "LengthStandard",
            ["ShaleShaker.MaxLimitOperatingCapacity"] = "VolumetricFlowrateDrilling",
            ["StandPipe.PressureMeasurementElevation"] = "LengthStandard",
            ["StandPipe.MudHoseHangingPointElevation"] = "LengthStandard",
            ["SurfaceMpdEquipment.MinimumBoreholeSize"] = "DiameterPipeDrilling",
            ["SurfaceMpdEquipment.MaximumBoreholeSize"] = "DiameterPipeDrilling",
            ["SurfaceMpdEquipment.PressureAccuracy"] = "PressureDrilling",
            ["SurfaceMpdEquipment.MaxLimitMudWeight"] = "MassDensityDrilling",
            ["TopDrive.Weight"] = "MassDrilling",
            ["TopDrive.ProportionalGain"] = "ProportionStandard",
            ["TopDrive.IntegralGain"] = "ProportionStandard",
            ["TopDrive.TorqueHighPassFilterTimeConstant"] = "DurationDrilling",
            ["TopDrive.TorqueLowPassFilterTimeConstant"] = "DurationDrilling",
            ["TopDrive.VFDFilterTimeConstant"] = "DurationDrilling",
            ["TopDrive.EncoderTimeConstant"] = "DurationDrilling",
            ["TopDrive.AccelerationFilterTimeConstant"] = "DurationDrilling",
            ["TorqueTurnSub.Weight"] = "MassDrilling",
            ["TorqueTurnSub.BatteryLife"] = "DurationDrilling",
            ["TravellingBlock.Weight"] = "MassDrilling",
            ["TravellingBlock.GrooveDiameter"] = "CableDiameterDrilling",
            ["TravellingBlock.MaxLimitBlockTravel"] = "LengthStandard",
            ["BopStack.Weight"] = "MassDrilling",
            ["EquipmentMeasurementCapability.RelativeAccuracy"] = "ProportionStandard",
            ["EquipmentMeasurementCapability.UpdateFrequency"] = "Frequency"
        };

    private static readonly IReadOnlyDictionary<string, string> QuantityDescriptions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AccelerationDrilling"] = "metre per second squared (m/s²)",
            ["AngularVelocityDrilling"] = "radian per second (rad/s)",
            ["AxialVelocityDrilling"] = "metre per second (m/s)",
            ["CableDiameterDrilling"] = "metre (m)",
            ["ChokeOpeningRateDrilling"] = "proportion per second (1/s)",
            ["DepthDrilling"] = "metre (m)",
            ["DiameterPipeDrilling"] = "metre (m)",
            ["DurationDrilling"] = "second (s)",
            ["ElectricTension"] = "volt (V)",
            ["Force"] = "newton (N)",
            ["ForceDrilling"] = "newton (N)",
            ["Frequency"] = "hertz (Hz)",
            ["HookLoadDrilling"] = "newton (N)",
            ["Length"] = "metre (m)",
            ["LengthStandard"] = "metre (m)",
            ["MassDensityDrilling"] = "kilogram per cubic metre (kg/m³)",
            ["MassDrilling"] = "kilogram (kg)",
            ["MassGradientPerLengthDrilling"] = "kilogram per metre (kg/m)",
            ["PlaneAngle"] = "radian (rad)",
            ["PowerDrilling"] = "watt (W)",
            ["PowerRateOfChangeDrilling"] = "watt per second (W/s)",
            ["PressureDrilling"] = "pascal (Pa)",
            ["ProportionStandard"] = "dimensionless SI ratio",
            ["RateOfPenetrationDrilling"] = "metre per second (m/s)",
            ["RotationalFrequencyRateOfChangeDrilling"] = "hertz per second (Hz/s)",
            ["StrokeFrequency"] = "hertz (Hz)",
            ["TemperatureDrilling"] = "kelvin (K)",
            ["TorqueDrilling"] = "newton metre (N·m)",
            ["Velocity"] = "metre per second (m/s)",
            ["VolumeDrilling"] = "cubic metre (m³)",
            ["VolumetricFlowrateDrilling"] = "cubic metre per second (m³/s)",
            ["WeightOnBitDrilling"] = "newton (N)"
        };

    public static JsonObject Metadata(string concept, string? role = null, string? reference = null) =>
        SemanticMetadata.Create(concept, role, reference, Catalogue.OsdcCanonicalDrilling,
            assertionSource: "rig-provider-binding-registry");

    public static JsonObject? ForType(Type type) =>
        TypeConcepts.TryGetValue(type.Name, out string? concept) ? Metadata(concept) : null;

    public static JsonObject? ForProperty(PropertyInfo property) =>
        ForProperty(property.DeclaringType ?? throw new InvalidOperationException("A declaring type is required."), property.Name);

    public static JsonObject? ForProperty(Type declaringType, string propertyName)
    {
        if(declaringType.Name is "Rig" or "RigLight" or "RigReadResponse" or "MetaInfo") {
            var common=propertyName switch{
                "MetaInfo"=>Metadata(Concepts.ResourceMetadata),"ID"=>Metadata(Concepts.ResourceIdentifier),
                "Name"=>Metadata(Concepts.ResourceName),"Description"=>Metadata(Concepts.ResourceDescription),
                "CreationDate"=>Metadata(Concepts.Instant,Concepts.CreationTime,Concepts.Utc),
                "LastModificationDate"=>Metadata(Concepts.Instant,Concepts.LastModificationTime,Concepts.Utc),_=>null};
            if(common is not null)return common;
        }
        Binding? binding = BindingFor(declaringType.Name, propertyName);
        return binding is null ? null : Metadata(binding.Concept, binding.Role, binding.Reference);
    }

    public static JsonObject? NestedBindings(PropertyInfo property)
    {
        if (property.DeclaringType?.Name != "FixedPlatformProperties" || property.Name != "DrillFloorDepth") return null;
        return new JsonObject
        {
            ["/GaussianValue/Mean"] = Metadata(Concepts.DrillFloorDepth, Concepts.ExpectedValue),
            ["/GaussianValue/StandardDeviation"] = Metadata(Concepts.LinearStandardUncertainty),
            ["/GaussianValue/MinValue"] = Metadata(Concepts.DrillFloorDepth, Concepts.DistributionLowerBound),
            ["/GaussianValue/MaxValue"] = Metadata(Concepts.DrillFloorDepth, Concepts.DistributionUpperBound)
        };
    }

    public static string QuantityName(Type declaringType, string propertyName)
    {
        string key = $"{declaringType.Name}.{propertyName}";
        if (PropertyQuantities.TryGetValue(key, out string? quantity)) return quantity;
        string name = propertyName.ToLowerInvariant();
        if (name == "weight" || name == "mass" || name == "operatingdisplacement" || name == "maximummass") return "MassDrilling";
        if (name.Contains("mudweight") || name.Contains("density")) return "MassDensityDrilling";
        if (name.Contains("pressure")) return "PressureDrilling";
        if (name.Contains("temperature")) return "TemperatureDrilling";
        if (name.Contains("torque") || name.EndsWith("trq")) return "TorqueDrilling";
        if (name.Contains("power")) return "PowerDrilling";
        if (name.Contains("efficiency") || name.Contains("factor") || name.Contains("gain") || name.Contains("cvvalue") || name.Contains("clogging")) return "ProportionStandard";
        if (name.Contains("flowrate") || name.Contains("flow") || name.Contains("pumprate")) return "VolumetricFlowrateDrilling";
        if (name.Contains("volume")) return "VolumeDrilling";
        if (name.Contains("windspeed") || name.Contains("transitspeed") || name.Contains("coilspeed")) return "Velocity";
        if (name.Contains("speed") || name.Contains("rotation")) return "AngularVelocityDrilling";
        if (name.Contains("frequency") || name.Contains("updaterate")) return "Frequency";
        if (name.Contains("acceleration")) return "AccelerationDrilling";
        if (name.Contains("force") || name.Contains("load") || name.Contains("hook") || name.Contains("tension") || name.Contains("capacity")) return "ForceDrilling";
        if (name.Contains("angle") || name.Contains("orientation") || name.Contains("azimuth")) return "PlaneAngle";
        if (name.Contains("time") || name.Contains("batterylife")) return "DurationDrilling";
        if (name.Contains("diameter") || name.EndsWith("od") || name.EndsWith("id") || name.Contains("bushingsize")) return "DiameterPipeDrilling";
        if (name.Contains("radius") || name.Contains("height") || name.Contains("length") || name.Contains("depth") ||
            name.Contains("elevation") || name.Contains("position") || name.Contains("clearance") || name.Contains("stroke")) return "LengthStandard";
        return "Dimensionless";
    }

    public static string NumericDescription(PropertyInfo property, string label)
    {
        if (property.DeclaringType?.Name == "EquipmentMeasurementCapability" &&
            property.Name is "MinimumValue" or "MaximumValue" or "AbsoluteAccuracy")
            return $"{label} in the SI unit identified by the sibling PhysicalQuantity value; do not send a display-unit value.";
        string quantity = QuantityName(property.DeclaringType!, property.Name);
        string unit = QuantityDescriptions.TryGetValue(quantity, out string? known) ? known : "the canonical SI unit";
        return $"{label} in {unit}, physical quantity {quantity}; do not send a display-unit value.";
    }

    private static Binding? BindingFor(string type, string property)
    {
        string key = $"{type}.{property}";
        if (key == "Rig.RigType") return new(Concepts.RigType);
        if (key == "Rig.OperatingEnvironment") return new(Concepts.RigOperatingEnvironment);
        if (key == "Rig.MobilityType") return new(Concepts.RigMobilityType);
        if (key == "RigEquipmentBase.LifecycleStatus") return new(Concepts.EquipmentLifecycleStatus);
        if (key is "StandPipe.PressureMeasurementElevation" or "StandPipe.MudHoseHangingPointElevation")
            return new(Concepts.Elevation, Reference: Concepts.DrillFloorUpward);
        if (key == "RigOperatingEnvelope.MaximumDrillingDepth") return new(Concepts.DrillingDepthCapacity, Concepts.MaximumOperatingLimit);
        if (key == "RigOperatingEnvelope.MaximumWaterDepth") return new(Concepts.WaterDepthCapacity, Concepts.MaximumOperatingLimit);
        if (key is "DrillstringHeaveCompensator.CompensatorCapacity" or "RiserHeaveCompensator.CompensatorCapacity")
            return new(Concepts.ForceCapacity, Concepts.RatedValue);
        if (key is "TopDrive.ProportionalGain" or "TopDrive.IntegralGain") return new(Concepts.ControllerGain);
        if (key == "Generator.Speed") return new(Concepts.RotationalAngularVelocity, Concepts.NominalValue);
        if (key is "Generator.StartupTimeCold") return new(Concepts.EquipmentDuration, Concepts.ColdStartDuration);
        if (key is "Generator.StartupTimeWarm") return new(Concepts.EquipmentDuration, Concepts.WarmStartDuration);
        if (key == "Generator.Voltage") return new(Concepts.ElectricalPotential, Concepts.NominalValue);
        if (key == "Generator.PowerFactor") return new(Concepts.EquipmentEfficiency, Concepts.NominalValue);
        if (key is "DrillingMarineRiser.JointWeight" or "DrillLine.LinearWeight") return new(Concepts.LinearMassDensity);
        if (property == "Weight" || property == "Mass" || key == "MarineUnitProfile.OperatingDisplacement")
            return new(Concepts.EquipmentMass);
        if (type == "EquipmentMeasurementCapability")
        {
            if (property == "MinimumValue") return new(Concepts.MeasurementRangeValue, Concepts.MeasurementRangeMinimum);
            if (property == "MaximumValue") return new(Concepts.MeasurementRangeValue, Concepts.MeasurementRangeMaximum);
            if (property == "AbsoluteAccuracy") return new(Concepts.AbsoluteMeasurementAccuracy);
            if (property == "RelativeAccuracy") return new(Concepts.RelativeMeasurementAccuracy);
            if (property == "UpdateFrequency") return new(Concepts.SignalFrequency);
        }
        if (key == "SurfaceMpdEquipment.PressureAccuracy") return new(Concepts.AbsoluteMeasurementAccuracy);
        if (key is "AutoDriller.MaxLimitDifferentialPressure" or "AutoDriller.MinLimitDifferentialPressure")
            return new(Concepts.PressureDifference, LimitRole(property));
        if (property.Contains("Pressure", StringComparison.Ordinal))
            return new(Concepts.AbsolutePressureRating, LimitRole(property));
        if (property.Contains("RotationSpeed", StringComparison.Ordinal) || property.Contains("RotatingSpeed", StringComparison.Ordinal) ||
            key == "Generator.MaxLimitSpeed")
            return new(Concepts.RotationalAngularVelocity, LimitRole(property));
        if (key == "MudPump.MaxLimitOperatingSpeed") return new(Concepts.StrokeFrequency, Concepts.MaximumOperatingLimit);
        if (property.Contains("Power", StringComparison.Ordinal)) return new(Concepts.PowerRating, LimitRole(property));
        if (property.Contains("Flow", StringComparison.Ordinal) || property.Contains("PumpRate", StringComparison.Ordinal))
            return new(Concepts.VolumetricFlowCapacity, LimitRole(property));
        if (property.Contains("Volume", StringComparison.Ordinal)) return new(Concepts.VolumeCapacity, LimitRole(property));
        return null;
    }

    private static string? LimitRole(string property)
    {
        if (property.Contains("DischargePressure", StringComparison.Ordinal)) return Concepts.DischargePressureLimit;
        if (property.Contains("ActivationPressure", StringComparison.Ordinal)) return Concepts.ActivationPressureLimit;
        if (property.Contains("StaticPressure", StringComparison.Ordinal)) return Concepts.StaticPressureLimit;
        if (property.Contains("DynamicPressure", StringComparison.Ordinal)) return Concepts.DynamicPressureLimit;
        if (property.Contains("Survival", StringComparison.Ordinal)) return Concepts.SurvivalLimit;
        if (property.Contains("Design", StringComparison.Ordinal)) return Concepts.MaximumDesignLimit;
        if (property.StartsWith("Min", StringComparison.Ordinal) || property.StartsWith("Minimum", StringComparison.Ordinal))
            return Concepts.MinimumOperatingLimit;
        if (property.Contains("Test", StringComparison.Ordinal)) return Concepts.MaximumTestLimit;
        if (property.Contains("Continuous", StringComparison.Ordinal)) return Concepts.ContinuousRating;
        if (property.Contains("Intermittent", StringComparison.Ordinal)) return Concepts.IntermittentRating;
        if (property.Contains("Makeup", StringComparison.Ordinal)) return Concepts.MakeUpLimit;
        if (property.Contains("Breakout", StringComparison.Ordinal)) return Concepts.BreakoutLimit;
        if (property.StartsWith("Max", StringComparison.Ordinal) || property.StartsWith("Maximum", StringComparison.Ordinal))
            return Concepts.MaximumOperatingLimit;
        if (property.StartsWith("Rated", StringComparison.Ordinal)) return Concepts.RatedValue;
        return null;
    }
}
