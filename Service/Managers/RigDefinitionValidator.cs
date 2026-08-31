using OSDC.Drilling.Rig.Model;
using System.Collections;
using System.Reflection;

namespace OSDC.Drilling.Rig.Service.Managers;

internal static class RigDefinitionValidator
{
    public static List<string> Validate(Model.Rig rig)
    {
        List<string> errors = [];
        if (rig.IsFixedPlatform && (!rig.ClusterID.HasValue || rig.ClusterID == Guid.Empty))
            errors.Add("ClusterID must be a non-empty UUID when IsFixedPlatform is true.");
        if (!rig.IsFixedPlatform && rig.ClusterID.HasValue)
            errors.Add("ClusterID must be null when IsFixedPlatform is false.");
        ValidateIdentification(rig.Identification, errors);
        ValidateEnvelope(rig.OperatingEnvelope, errors);
        ValidateMarine(rig.MarineUnitProfile, errors);
        ValidateJackUp(rig.JackUpProfile, errors);
        ValidateStationKeeping(rig.StationKeepingSystem, errors);
        ValidateMudPumps(rig.MudPumpList, errors);
        ValidateEquipmentMeasurements(rig, errors);
        foreach ((RigStorageCapacity value, int index) in (rig.StorageCapacities ?? []).Select((value, index) => (value, index)))
        {
            NonNegative(value.MaximumVolume, $"StorageCapacities[{index}].MaximumVolume", errors);
            NonNegative(value.MaximumMass, $"StorageCapacities[{index}].MaximumMass", errors);
            if (value.MaximumVolume is null && value.MaximumMass is null)
                errors.Add($"StorageCapacities[{index}] must define MaximumVolume or MaximumMass.");
        }
        return errors;
    }

    private static void ValidateEquipmentMeasurements(Model.Rig rig, List<string> errors)
    {
        List<(RigEquipmentBase Equipment, string Path)> equipment = EnumerateEquipment(rig, "Rig", []).ToList();
        HashSet<Guid> componentIds = EnumerateComponents(rig, []).Where(value => value.ID.HasValue).Select(value => value.ID!.Value).ToHashSet();

        foreach ((RigEquipmentBase item, string equipmentPath) in equipment)
        {
            HashSet<string> codes = new(StringComparer.OrdinalIgnoreCase);
            foreach ((EquipmentMeasurementCapability capability, int index) in (item.MeasurementCapabilities ?? []).Select((value, index) => (value, index)))
            {
                string path = $"{equipmentPath}.MeasurementCapabilities[{index}]";
                if (string.IsNullOrWhiteSpace(capability.MeasurementCode))
                    errors.Add($"{path}.MeasurementCode is required.");
                else if (!codes.Add(capability.MeasurementCode.Trim()))
                    errors.Add($"{path}.MeasurementCode duplicates another capability on this equipment.");
                if (string.IsNullOrWhiteSpace(capability.PhysicalQuantity))
                    errors.Add($"{path}.PhysicalQuantity is required.");
                if (capability.SourceKind is null)
                    errors.Add($"{path}.SourceKind is required.");
                if (capability.SourceKind == MeasurementSourceKind.Sensor && string.IsNullOrWhiteSpace(capability.SourceType))
                    errors.Add($"{path}.SourceType is required for a sensor measurement.");
                if (capability.SourceComponentID is Guid sourceId && !componentIds.Contains(sourceId))
                    errors.Add($"{path}.SourceComponentID does not identify a component in this rig.");

                FiniteWhenDefined(capability.MinimumValue, $"{path}.MinimumValue", errors);
                FiniteWhenDefined(capability.MaximumValue, $"{path}.MaximumValue", errors);
                NonNegative(capability.AbsoluteAccuracy, $"{path}.AbsoluteAccuracy", errors);
                NonNegative(capability.RelativeAccuracy, $"{path}.RelativeAccuracy", errors);
                PositiveWhenDefined(capability.UpdateFrequency, $"{path}.UpdateFrequency", errors);
                if (capability.RelativeAccuracy > 1)
                    errors.Add($"{path}.RelativeAccuracy must not exceed 1.");
                if (capability.MinimumValue is not null && capability.MaximumValue is not null && capability.MinimumValue > capability.MaximumValue)
                    errors.Add($"{path}.MinimumValue must not exceed MaximumValue.");
            }
        }
    }

    private static IEnumerable<(RigEquipmentBase Equipment, string Path)> EnumerateEquipment(object? value, string path, HashSet<object> visited)
    {
        if (!CanTraverse(value) || !visited.Add(value!)) yield break;
        if (value is RigEquipmentBase equipment) yield return (equipment, path);
        if (value is IEnumerable sequence)
        {
            int index = 0;
            foreach (object? item in sequence)
            {
                foreach (var result in EnumerateEquipment(item, $"{path}[{index}]", visited)) yield return result;
                index++;
            }
            yield break;
        }
        foreach (PropertyInfo property in value!.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.CanRead && property.GetIndexParameters().Length == 0))
        {
            foreach (var result in EnumerateEquipment(property.GetValue(value), $"{path}.{property.Name}", visited)) yield return result;
        }
    }

    private static IEnumerable<RigComponentBase> EnumerateComponents(object? value, HashSet<object> visited)
    {
        if (!CanTraverse(value) || !visited.Add(value!)) yield break;
        if (value is RigComponentBase component) yield return component;
        if (value is IEnumerable sequence)
        {
            foreach (object? item in sequence)
                foreach (RigComponentBase result in EnumerateComponents(item, visited)) yield return result;
            yield break;
        }
        foreach (PropertyInfo property in value!.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.CanRead && property.GetIndexParameters().Length == 0))
            foreach (RigComponentBase result in EnumerateComponents(property.GetValue(value), visited)) yield return result;
    }

    private static bool CanTraverse(object? value)
    {
        if (value is null || value is string) return false;
        Type type = value.GetType();
        return value is IEnumerable || type.Namespace?.StartsWith("OSDC.Drilling.Rig.Model", StringComparison.Ordinal) == true;
    }

    private static void ValidateMudPumps(IReadOnlyList<MudPump>? pumps, List<string> errors)
    {
        foreach ((MudPump pump, int pumpIndex) in (pumps ?? []).Select((value, index) => (value, index)))
        {
            PositiveWhenDefined(pump.Stroke, $"MudPumpList[{pumpIndex}].Stroke", errors);
            HashSet<double> linerDiameters = [];
            foreach ((MudPumpLinerConfiguration liner, int linerIndex) in (pump.LinerConfigurations ?? []).Select((value, index) => (value, index)))
            {
                string path = $"MudPumpList[{pumpIndex}].LinerConfigurations[{linerIndex}]";
                RequiredPositive(liner.LinerInnerDiameter, $"{path}.LinerInnerDiameter", errors);
                RequiredPositive(liner.MaximumVolumetricFlowRate, $"{path}.MaximumVolumetricFlowRate", errors);
                RequiredPositive(liner.MaximumDischargePressure, $"{path}.MaximumDischargePressure", errors);
                PositiveWhenDefined(liner.DisplacementPerStroke, $"{path}.DisplacementPerStroke", errors);
                if (liner.LinerInnerDiameter is double diameter && !linerDiameters.Add(diameter))
                    errors.Add($"{path}.LinerInnerDiameter duplicates another liner configuration for this pump.");
                if (pump.MaxLimitDesignPressure is double designPressure && liner.MaximumDischargePressure > designPressure)
                    errors.Add($"{path}.MaximumDischargePressure must not exceed the pump MaxLimitDesignPressure.");
            }
        }
    }

    private static void ValidateIdentification(RigIdentification? value, List<string> errors)
    {
        if (value is null) return;
        int maximumYear = DateTime.UtcNow.Year + 2;
        if (value.YearBuilt is < 1800 || value.YearBuilt > maximumYear) errors.Add($"Identification.YearBuilt must be between 1800 and {maximumYear}.");
        if (value.YearEnteredService is < 1800 || value.YearEnteredService > maximumYear) errors.Add($"Identification.YearEnteredService must be between 1800 and {maximumYear}.");
        if (value.YearBuilt is not null && value.YearEnteredService is not null && value.YearEnteredService < value.YearBuilt)
            errors.Add("Identification.YearEnteredService must not precede YearBuilt.");
        foreach ((RigExternalIdentifier identifier, int index) in (value.ExternalIdentifiers ?? []).Select((item, index) => (item, index)))
            if (string.IsNullOrWhiteSpace(identifier.Authority) || string.IsNullOrWhiteSpace(identifier.Identifier))
                errors.Add($"Identification.ExternalIdentifiers[{index}] requires both Authority and Identifier.");
    }

    private static void ValidateEnvelope(RigOperatingEnvelope? value, List<string> errors)
    {
        if (value is null) return;
        NonNegative(value.MaximumDrillingDepth, "OperatingEnvelope.MaximumDrillingDepth", errors);
        NonNegative(value.MaximumWaterDepth, "OperatingEnvelope.MaximumWaterDepth", errors);
        NonNegative(value.RatedHookLoad, "OperatingEnvelope.RatedHookLoad", errors);
        NonNegative(value.MaximumSetbackLoad, "OperatingEnvelope.MaximumSetbackLoad", errors);
        NonNegative(value.MaximumRotaryLoad, "OperatingEnvelope.MaximumRotaryLoad", errors);
        NonNegative(value.MaximumMudSystemPressure, "OperatingEnvelope.MaximumMudSystemPressure", errors);
        NonNegative(value.MinimumAmbientTemperature, "OperatingEnvelope.MinimumAmbientTemperature", errors);
        NonNegative(value.MaximumAmbientTemperature, "OperatingEnvelope.MaximumAmbientTemperature", errors);
        NonNegative(value.MaximumOperatingWindSpeed, "OperatingEnvelope.MaximumOperatingWindSpeed", errors);
        NonNegative(value.MaximumSurvivalWindSpeed, "OperatingEnvelope.MaximumSurvivalWindSpeed", errors);
        if (value.MinimumAmbientTemperature is not null && value.MaximumAmbientTemperature is not null && value.MinimumAmbientTemperature > value.MaximumAmbientTemperature)
            errors.Add("OperatingEnvelope.MinimumAmbientTemperature must not exceed MaximumAmbientTemperature.");
    }

    private static void ValidateMarine(MarineUnitProfile? value, List<string> errors)
    {
        if (value is null) return;
        NonNegative(value.HullLength, "MarineUnitProfile.HullLength", errors);
        NonNegative(value.HullWidth, "MarineUnitProfile.HullWidth", errors);
        NonNegative(value.HullDepth, "MarineUnitProfile.HullDepth", errors);
        NonNegative(value.OperatingDraft, "MarineUnitProfile.OperatingDraft", errors);
        NonNegative(value.TransitDraft, "MarineUnitProfile.TransitDraft", errors);
        NonNegative(value.OperatingDisplacement, "MarineUnitProfile.OperatingDisplacement", errors);
        NonNegative(value.VariableDeckLoad, "MarineUnitProfile.VariableDeckLoad", errors);
        NonNegative(value.MaximumTransitSpeed, "MarineUnitProfile.MaximumTransitSpeed", errors);
        if (value.AccommodationCapacity < 0) errors.Add("MarineUnitProfile.AccommodationCapacity must be non-negative.");
        if (value.CraneCount < 0) errors.Add("MarineUnitProfile.CraneCount must be non-negative.");
    }

    private static void ValidateJackUp(JackUpProfile? value, List<string> errors)
    {
        if (value is null) return;
        NonNegative(value.LegLength, "JackUpProfile.LegLength", errors);
        NonNegative(value.LongitudinalLegSpacing, "JackUpProfile.LongitudinalLegSpacing", errors);
        NonNegative(value.TransverseLegSpacing, "JackUpProfile.TransverseLegSpacing", errors);
        NonNegative(value.MaximumCantileverSkidOut, "JackUpProfile.MaximumCantileverSkidOut", errors);
        NonNegative(value.MaximumCantileverTransverseReach, "JackUpProfile.MaximumCantileverTransverseReach", errors);
        NonNegative(value.SubstructureTravel, "JackUpProfile.SubstructureTravel", errors);
        NonNegative(value.MaximumPreload, "JackUpProfile.MaximumPreload", errors);
    }

    private static void ValidateStationKeeping(StationKeepingSystem? value, List<string> errors)
    {
        if (value is null) return;
        if (value.ThrusterCount < 0) errors.Add("StationKeepingSystem.ThrusterCount must be non-negative.");
        if (value.MooringLineCount < 0) errors.Add("StationKeepingSystem.MooringLineCount must be non-negative.");
        NonNegative(value.MaximumMooringLineTension, "StationKeepingSystem.MaximumMooringLineTension", errors);
        if (value.DynamicPositioningClass is not null and not DynamicPositioningClass.None && value.Modes?.Contains(StationKeepingMode.DynamicPositioning) != true)
            errors.Add("StationKeepingSystem.Modes must include DynamicPositioning when a DP class is specified.");
    }

    private static void NonNegative(double? value, string property, List<string> errors)
    {
        if (value is < 0 || double.IsNaN(value ?? 0) || double.IsInfinity(value ?? 0)) errors.Add($"{property} must be a finite, non-negative SI value.");
    }

    private static void RequiredPositive(double? value, string property, List<string> errors)
    {
        if (value is null || value <= 0 || double.IsNaN(value.Value) || double.IsInfinity(value.Value))
            errors.Add($"{property} is required and must be a finite, positive SI value.");
    }

    private static void PositiveWhenDefined(double? value, string property, List<string> errors)
    {
        if (value is not null && (value <= 0 || double.IsNaN(value.Value) || double.IsInfinity(value.Value)))
            errors.Add($"{property} must be a finite, positive SI value when defined.");
    }

    private static void FiniteWhenDefined(double? value, string property, List<string> errors)
    {
        if (value is not null && (double.IsNaN(value.Value) || double.IsInfinity(value.Value)))
            errors.Add($"{property} must be a finite SI value when defined.");
    }
}
