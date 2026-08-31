using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;
using OSDC.Drilling.Rig.Model;

namespace OSDC.Drilling.Rig.Service.Mcp.Tools;

internal static class McpToolArgumentHelpers
{
    private static readonly IReadOnlyDictionary<string, string> ScalarUnits =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AutoDriller.MaxLimitRop"] = "metre per second (m/s), physical quantity RateOfPenetrationDrilling",
            ["AutoDriller.MinLimitRop"] = "metre per second (m/s), physical quantity RateOfPenetrationDrilling",
            ["AutoDriller.MaxLimitWob"] = "newton (N), physical quantity WeightOnBitDrilling",
            ["AutoDriller.MinLimitWob"] = "newton (N), physical quantity WeightOnBitDrilling",
            ["AutoDriller.MaxLimitTrq"] = "newton metre (N·m), physical quantity TorqueDrilling",
            ["AutoDriller.MinLimitTrq"] = "newton metre (N·m), physical quantity TorqueDrilling",
            ["BopLineDefinition.LineId"] = "metre (m), physical quantity DiameterPipeDrilling",
            ["BopLineDefinition.LineOd"] = "metre (m), physical quantity DiameterPipeDrilling",
            ["CasingDriveSystem.HoistingCapacity"] = "newton (N), physical quantity HookLoadDrilling",
            ["CasingDriveSystem.MaxLimitPushDown"] = "newton (N), physical quantity ForceDrilling",
            ["CoilDriveSystem.ReelPayloadCapacity"] = "newton (N), physical quantity ForceDrilling",
            ["CoilDriveSystem.ReelPayloadLength"] = "metre (m), physical quantity LengthStandard",
            ["CoilDriveSystem.InjectorHeadMinTubingOd"] = "metre (m), physical quantity DiameterPipeDrilling",
            ["CoilDriveSystem.InjHeadDesignPullCapacity"] = "newton (N), physical quantity ForceDrilling",
            ["CoilDriveSystem.InjHeadDesignSnubCapacity"] = "newton (N), physical quantity ForceDrilling",
            ["CoilDriveSystem.InjHeadPullCapacity"] = "newton (N), physical quantity ForceDrilling",
            ["CoilDriveSystem.InjHeadSnubCapacity"] = "newton (N), physical quantity ForceDrilling",
            ["CoilDriveSystem.InjHeadMaxSpeed"] = "metre per second (m/s), physical quantity AxialVelocityDrilling",
            ["ContinuousCirculationDevice.MaxLimitMudWeight"] = "kilogram per cubic metre (kg/m³), physical quantity MassDensityDrilling",
            ["ContinuousCirculationDevice.MaxLimitRotationRate"] = "radian per second (rad/s), physical quantity AngularVelocityDrilling",
            ["CrownBlock.GrooveDiameter"] = "metre (m), physical quantity CableDiameterDrilling",
            ["CrownBlock.MaxLimitCompensatorStroke"] = "metre (m), physical quantity LengthStandard",
            ["Derrick.MaxLimitWindSpeed"] = "metre per second (m/s), physical quantity Velocity",
            ["DrillingChokeManifold.MaxLimitOpeningSpeed"] = "proportion per second (1/s), physical quantity ChokeOpeningRateDrilling",
            ["DrillingChokeManifold.TrimSize"] = "metre (m), physical quantity DiameterPipeDrilling",
            ["DrillingChokeManifold.FlowMeterSize"] = "metre (m), physical quantity DiameterPipeDrilling",
            ["DrillingMarineRiser.JointWeight"] = "kilogram per metre (kg/m), physical quantity MassGradientPerLengthDrilling",
            ["DrillLine.Diameter"] = "metre (m), physical quantity CableDiameterDrilling",
            ["DrillLine.LinearWeight"] = "kilogram per metre (kg/m), physical quantity MassGradientPerLengthDrilling",
            ["DrillstringHeaveCompensator.MaxLimitCompensatorStroke"] = "metre (m), physical quantity LengthStandard",
            ["FlowRoutingManifold.FlangeSize"] = "metre (m), physical quantity DiameterPipeDrilling",
            ["FlowRoutingManifold.PressureReliefValveTrim"] = "metre (m), physical quantity DiameterPipeDrilling",
            ["Generator.PowerFactor"] = "dimensionless SI ratio, physical quantity ProportionStandard",
            ["Generator.StartupTimeCold"] = "second (s), physical quantity DurationDrilling",
            ["Generator.StartupTimeWarm"] = "second (s), physical quantity DurationDrilling",
            ["Generator.Voltage"] = "volt (V), physical quantity ElectricTension",
            ["Generator.MaxLimitVoltage"] = "volt (V), physical quantity ElectricTension",
            ["Generator.MinLimitVoltage"] = "volt (V), physical quantity ElectricTension",
            ["Generator.MaxLimitPowerIncrease"] = "watt per second (W/s), physical quantity PowerRateOfChangeDrilling",
            ["Generator.MaxLimitSpeedIncrease"] = "hertz per second (Hz/s), physical quantity RotationalFrequencyRateOfChangeDrilling",
            ["Generator.MaxLimitFrequency"] = "hertz (Hz), physical quantity Frequency",
            ["Generator.MinLimitFrequency"] = "hertz (Hz), physical quantity Frequency",
            ["MarineMpdEquipment.Weight"] = "kilogram (kg), physical quantity MassDrilling",
            ["MarineUnitProfile.MaximumTransitSpeed"] = "metre per second (m/s), physical quantity Velocity",
            ["MeasurementAfm.UpdateRate"] = "hertz (Hz), physical quantity Frequency",
            ["MpdControlDevice.NominalSize"] = "metre (m), physical quantity DiameterPipeDrilling",
            ["MpdController.PrimaryChokeTrim"] = "metre (m), physical quantity DiameterPipeDrilling",
            ["MpdController.SecondaryChokeTrim"] = "metre (m), physical quantity DiameterPipeDrilling",
            ["MudPump.MaxLimitOperatingSpeed"] = "hertz (Hz), physical quantity StrokeFrequency",
            ["CementPumpDisplacementPoint.StrokeRate"] = "hertz (Hz), physical quantity StrokeFrequency",
            ["Rig.DrillFloorElevation"] = "metre (m), physical quantity HeightDrilling",
            ["RigOperatingEnvelope.MaximumDrillingDepth"] = "metre (m), physical quantity DepthDrilling",
            ["RigOperatingEnvelope.MaximumWaterDepth"] = "metre (m), physical quantity DepthDrilling",
            ["RigOperatingEnvelope.MaximumOperatingWindSpeed"] = "metre per second (m/s), physical quantity Velocity",
            ["RigOperatingEnvelope.MaximumSurvivalWindSpeed"] = "metre per second (m/s), physical quantity Velocity",
            ["RiserHeaveCompensator.MaxLimitCompensatorStroke"] = "metre (m), physical quantity LengthStandard",
            ["ShaleShaker.MaxLimitOperatingCapacity"] = "cubic metre per second (m³/s), physical quantity VolumetricFlowrateDrilling",
            ["SurfaceMpdEquipment.MinimumBoreholeSize"] = "metre (m), physical quantity DiameterPipeDrilling",
            ["SurfaceMpdEquipment.MaximumBoreholeSize"] = "metre (m), physical quantity DiameterPipeDrilling",
            ["SurfaceMpdEquipment.MaxLimitMudWeight"] = "kilogram per cubic metre (kg/m³), physical quantity MassDensityDrilling",
            ["TopDrive.Weight"] = "kilogram (kg), physical quantity MassDrilling",
            ["TopDrive.TorqueHighPassFilterTimeConstant"] = "second (s), physical quantity DurationDrilling",
            ["TopDrive.TorqueLowPassFilterTimeConstant"] = "second (s), physical quantity DurationDrilling",
            ["TopDrive.VFDFilterTimeConstant"] = "second (s), physical quantity DurationDrilling",
            ["TopDrive.EncoderTimeConstant"] = "second (s), physical quantity DurationDrilling",
            ["TopDrive.AccelerationFilterTimeConstant"] = "second (s), physical quantity DurationDrilling",
            ["TorqueTurnSub.Weight"] = "kilogram (kg), physical quantity MassDrilling",
            ["TorqueTurnSub.BatteryLife"] = "second (s), physical quantity DurationDrilling",
            ["TravellingBlock.GrooveDiameter"] = "metre (m), physical quantity CableDiameterDrilling",
            ["TravellingBlock.MaxLimitBlockTravel"] = "metre (m), physical quantity LengthStandard",
            ["EquipmentMeasurementCapability.RelativeAccuracy"] = "dimensionless SI ratio, physical quantity ProportionStandard",
            ["EquipmentMeasurementCapability.UpdateFrequency"] = "hertz (Hz), physical quantity Frequency"
        };

    private static readonly IReadOnlyDictionary<string, string> PropertyDescriptions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MetaInfo"] = "Resource metadata. MetaInfo.ID is the persistent rig UUID and is supplied by the caller.",
            ["Name"] = "Human-readable name of the rig, mast, component, or equipment item.",
            ["Description"] = "Human-readable description of the rig, component, capabilities, or intended use.",
            ["CreationDate"] = "Creation timestamp in ISO 8601 format. Use a UTC offset where possible.",
            ["LastModificationDate"] = "Server-assigned last-modification timestamp in ISO 8601 format. Use the latest returned value as expectedModifiedUtc when replacing a stored rig.",
            ["IsFixedPlatform"] = "Whether this is a fixed-platform rig. When true, ClusterID should identify its Cluster; when false, ClusterID should be null.",
            ["ClusterID"] = "UUID of the Cluster hosting a fixed-platform rig. This is an external reference to the Cluster microservice, not an embedded Cluster object; leave null for non-fixed rigs.",
            ["DrillFloorElevation"] = "Drill-floor elevation in metres (m). The Rig payload stores only the SI scalar and no vertical-datum identifier, so callers must apply the configured depth-reference convention consistently.",
            ["MainRigMast"] = "Primary rig-mast assembly and its hoisting, rotary, pipe-handling, standpipe, choke, and related equipment.",
            ["AuxiliaryRigMast"] = "Optional secondary rig-mast assembly with the same nested equipment structure as MainRigMast.",
            ["MudPumpList"] = "Mud-circulation pumps installed on the rig, including equipment identity, pump class, displacement curve, and operating limits.",
            ["LinerConfigurations"] = "Ordered table of installable mud-pump liner sizes and their rated hydraulic performance. Use one row per distinct liner inner diameter.",
            ["LinerInnerDiameter"] = "Nominal mud-pump liner inner diameter in SI metres (m).",
            ["DisplacementPerStroke"] = "Theoretical or manufacturer-rated displaced volume per pump stroke in SI cubic metres (m3).",
            ["MaximumVolumetricFlowRate"] = "Maximum rated volumetric output for this liner at the pump's rated operating speed in SI cubic metres per second (m3/s).",
            ["MaximumDischargePressure"] = "Maximum rated discharge pressure for this liner in SI pascals (Pa); it must not exceed the pump design pressure.",
            ["CementPumpList"] = "Cement pumps installed on the rig, including displacement curve and pressure/flow limits.",
            ["MudTankList"] = "Mud tanks and their class, fluid type, and rated capacity.",
            ["GeneratorList"] = "Electrical generators and their engine, cooling, phase, speed, power, and electrical ratings.",
            ["ShaleShakerList"] = "Shale shakers and their classification, screen definitions, and rated capacity.",
            ["MeasurementCapabilities"] = "Installed sensor, manual, or calculated measurement capabilities. These definitions describe instrumentation only and never contain live measurement values.",
            ["MeasurementCode"] = "Stable machine-readable measurement name, for example standpipe_pressure. Codes must be unique within one equipment item.",
            ["PhysicalQuantity"] = "OSDC physical-quantity name that defines the SI unit for range and absolute-accuracy values.",
            ["SourceKind"] = "Measurement provenance: Sensor, Calculated, Manual, or Other.",
            ["SourceType"] = "Human-readable transducer, manual procedure, or calculation type. It is required when SourceKind is Sensor.",
            ["SourceComponentID"] = "Optional UUID of the component supplying the signal when it differs from the equipment containing this capability. The UUID must identify a component in the same rig.",
            ["MinimumValue"] = "Optional lower measurement-range boundary in the SI unit selected by PhysicalQuantity.",
            ["MaximumValue"] = "Optional upper measurement-range boundary in the SI unit selected by PhysicalQuantity.",
            ["AbsoluteAccuracy"] = "Optional non-negative absolute accuracy in the SI unit selected by PhysicalQuantity.",
            ["RelativeAccuracy"] = "Optional relative accuracy as a dimensionless fraction from 0 through 1; 0.01 means one percent.",
            ["UpdateFrequency"] = "Optional positive nominal measurement update frequency in hertz (Hz).",
            ["UnitReferences"] = "Ordered equipment unit or stack references. Use one string per reference.",
            ["Capabilities"] = "Ordered human-readable equipment capabilities. Use one string per capability.",
            ["ErrorSourceList"] = "Nested model items associated with this resource.",
            ["Manufacturer"] = "Equipment manufacturer.",
            ["Model"] = "Manufacturer's model designation.",
            ["ProductCode"] = "Manufacturer or operator product code.",
            ["SerialNumber"] = "Equipment serial number.",
            ["ID"] = "Non-empty UUID identifying the resource. Generate this before create; the service does not assign it.",
            ["HttpHostName"] = "Optional source-service host metadata retained for compatibility with the shared resource model.",
            ["HttpHostBasePath"] = "Optional source-service base-path metadata retained for compatibility with the shared resource model.",
            ["HttpEndPoint"] = "Optional source-service endpoint metadata retained for compatibility with the shared resource model."
        };

    public static JsonObject CreateEmptySchema() => new()
    {
        ["type"] = "object", ["properties"] = new JsonObject(), ["additionalProperties"] = false
    };

    public static JsonObject CreateGuidSchema(string key, string description) => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject { [key] = new JsonObject { ["type"] = "string", ["format"] = "uuid", ["description"] = description } },
        ["required"] = new JsonArray(key),
        ["additionalProperties"] = false
    };

    public static JsonObject CreateRigReadSchema(bool includeId)
    {
        var properties = new JsonObject
        {
            ["includePhotos"] = new JsonObject
            {
                ["type"] = "boolean",
                ["default"] = false,
                ["description"] = "When true, include photo metadata such as title, media type, byte length, checksum, attribution, and photo UUID. Image bytes are never returned by MCP."
            }
        };
        var schema = new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["additionalProperties"] = false
        };
        if (includeId)
        {
            properties["id"] = new JsonObject { ["type"] = "string", ["format"] = "uuid", ["description"] = "UUID of the rig to retrieve." };
            schema["required"] = new JsonArray("id");
        }
        return schema;
    }

    public static JsonObject CreateRigSchema(bool includeId = false)
    {
        var definitions = new JsonObject();
        JsonObject rigSchema = RequiredTypeSchema(typeof(Model.Rig), definitions);
        rigSchema["description"] = "Complete Rig master-data representation. JSON property names are case-sensitive and use PascalCase. Optional equipment may be null or omitted; specifications and limits use SI values. MeasurementCapabilities describe available instrumentation and never carry live telemetry.";

        var properties = new JsonObject { ["rig"] = rigSchema };
        var required = new JsonArray("rig");
        if (includeId)
        {
            properties["id"] = new JsonObject
            {
                ["type"] = "string", ["format"] = "uuid",
                ["description"] = "UUID of the persisted rig. It must exactly equal rig.MetaInfo.ID."
            };
            required.Add("id");
            properties["expectedModifiedUtc"] = new JsonObject
            {
                ["type"] = "string", ["format"] = "date-time",
                ["description"] = "LastModificationDate returned by the latest rig read. The service rejects a stale value with conflict."
            };
            required.Add("expectedModifiedUtc");
        }

        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = properties,
            ["required"] = required,
            ["additionalProperties"] = false,
            ["$defs"] = definitions
        };
    }

    public static JsonObject CreateFeatureCategorySchema(bool includeUpdateFields = false)
    {
        var definitions = new JsonObject();
        JsonObject categorySchema = RequiredTypeSchema(typeof(RigFeatureCategory), definitions);
        categorySchema["description"] = "Rig feature category. The service generates UUIDs for custom categories and new options; built-in definitions are immutable.";
        var properties = new JsonObject { ["category"] = categorySchema };
        var required = new JsonArray("category");
        if (includeUpdateFields)
        {
            properties["id"] = new JsonObject { ["type"] = "string", ["format"] = "uuid", ["description"] = "UUID of the custom category to replace." };
            properties["expectedModifiedUtc"] = new JsonObject { ["type"] = "string", ["format"] = "date-time", ["description"] = "LastModificationDate returned by the preceding read, used for optimistic concurrency." };
            required.Add("id");
            required.Add("expectedModifiedUtc");
        }
        return new JsonObject { ["type"] = "object", ["properties"] = properties, ["required"] = required, ["additionalProperties"] = false, ["$defs"] = definitions };
    }

    public static JsonObject CreateBatchExportSchema() => CreateBodySchema(
        "request", typeof(RigBatchExportRequest),
        "Select All for every rig in stable UUID order, or Selected with an explicitly ordered, non-empty RigIDs array.");

    public static JsonObject CreateBatchRestoreSchema() => CreateBodySchema(
        "request", typeof(RigBatchRestoreRequest),
        "Complete atomic restore request containing the versioned export document and explicit catalog and UUID-conflict policies.");

    public static JsonObject CreateStatusOutputSchema() => new()
    {
        ["type"] = "object",
        ["properties"] = new JsonObject { ["status"] = SuccessStatusSchema() },
        ["required"] = new JsonArray("status"),
        ["additionalProperties"] = false
    };

    public static JsonObject CreateResourceOutputSchema(Type dataType, bool collection = false)
    {
        JsonObject definitions = new();
        JsonObject dataSchema = collection
            ? new JsonObject { ["type"] = "array", ["items"] = TypeSchema(dataType, definitions, nullable: false) }
            : RequiredTypeSchema(dataType, definitions);
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject { ["status"] = SuccessStatusSchema(), ["data"] = dataSchema },
            ["required"] = new JsonArray("status", "data"),
            ["additionalProperties"] = false,
            ["$defs"] = definitions
        };
    }

    private static JsonObject SuccessStatusSchema() => new() { ["type"] = "integer", ["minimum"] = 200, ["maximum"] = 299 };

    private static JsonObject RequiredTypeSchema(Type type, JsonObject definitions)
    {
        JsonObject schema = TypeSchema(type, definitions, nullable: false);
        if (schema["anyOf"] is JsonArray alternatives && alternatives.Count == 2 &&
            alternatives[0] is JsonObject reference && alternatives[1]?["type"]?.GetValue<string>() == "null")
        {
            return (JsonObject)reference.DeepClone();
        }
        return schema;
    }

    private static JsonObject CreateBodySchema(string propertyName, Type bodyType, string description)
    {
        JsonObject definitions = new();
        JsonObject bodySchema = RequiredTypeSchema(bodyType, definitions);
        bodySchema["description"] = description;
        return new JsonObject
        {
            ["type"] = "object",
            ["properties"] = new JsonObject { [propertyName] = bodySchema },
            ["required"] = new JsonArray(propertyName),
            ["additionalProperties"] = false,
            ["$defs"] = definitions
        };
    }

    private static JsonObject TypeSchema(Type declaredType, JsonObject definitions, bool nullable)
    {
        Type? underlying = Nullable.GetUnderlyingType(declaredType);
        Type type = underlying ?? declaredType;
        nullable |= underlying is not null || (!type.IsValueType && type != typeof(string));

        if (type == typeof(string)) return Primitive("string", nullable);
        if (type == typeof(bool)) return Primitive("boolean", nullable);
        if (type == typeof(Guid)) return Primitive("string", nullable, "uuid");
        if (type == typeof(DateTimeOffset) || type == typeof(DateTime)) return Primitive("string", nullable, "date-time");
        if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(uint) || type == typeof(ulong)) return Primitive("integer", nullable);
        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal)) return Primitive("number", nullable);

        if (type.IsEnum)
        {
            var values = new JsonArray();
            foreach (string value in Enum.GetNames(type)) values.Add(value);
            var enumSchema = new JsonObject { ["type"] = "string", ["enum"] = values };
            return nullable ? NullableReference(enumSchema) : enumSchema;
        }

        Type? itemType = CollectionItemType(type);
        if (itemType is not null)
        {
            var arraySchema = new JsonObject
            {
                ["type"] = nullable ? new JsonArray("array", "null") : "array",
                ["items"] = TypeSchema(itemType, definitions, nullable: !itemType.IsValueType)
            };
            return arraySchema;
        }

        string definitionName = type.Name;
        if (!definitions.ContainsKey(definitionName))
        {
            definitions[definitionName] = new JsonObject();
            var properties = new JsonObject();
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.CanRead && p.CanWrite))
            {
                JsonObject propertySchema = TypeSchema(property.PropertyType, definitions, nullable: false);
                propertySchema["description"] = DescribeProperty(property);
                properties[property.Name] = propertySchema;
            }

            var definition = new JsonObject
            {
                ["type"] = "object",
                ["description"] = DescribeType(type),
                ["properties"] = properties,
                ["additionalProperties"] = false
            };
            if (type == typeof(Model.Rig)) definition["required"] = new JsonArray("MetaInfo");
            if (type.Name == "MetaInfo") definition["required"] = new JsonArray("ID");
            definitions[definitionName] = definition;
        }

        var reference = new JsonObject { ["$ref"] = $"#/$defs/{definitionName}" };
        return nullable ? NullableReference(reference) : reference;
    }

    private static JsonObject Primitive(string type, bool nullable, string? format = null)
    {
        var schema = new JsonObject { ["type"] = nullable ? new JsonArray(type, "null") : type };
        if (format is not null) schema["format"] = format;
        return schema;
    }

    private static JsonObject NullableReference(JsonObject schema) => new()
    {
        ["anyOf"] = new JsonArray(schema, new JsonObject { ["type"] = "null" })
    };

    private static Type? CollectionItemType(Type type)
    {
        if (type == typeof(string) || !typeof(IEnumerable).IsAssignableFrom(type)) return null;
        if (type.IsArray) return type.GetElementType();
        return type.GetInterfaces().Append(type)
            .FirstOrDefault(candidate => candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            ?.GetGenericArguments()[0];
    }

    private static string DescribeType(Type type)
    {
        if (type == typeof(Model.Rig)) return "Complete rig configuration, containing identity, platform/cluster association, drill-floor elevation, mast assemblies, and installed drilling equipment.";
        if (type == typeof(RigFeatureCategory)) return "User-extensible rig capability category with stable options, exclusivity, provenance, and optional assignment validity periods.";
        if (type == typeof(RigFeatureOption)) return "One selectable option within a rig feature category.";
        if (type == typeof(RigFeatureAssignment)) return "Assignment of one stored rig feature option to a rig, optionally with validity and evidence.";
        if (type == typeof(EquipmentMeasurementCapability)) return "Installed or calculated measurement capability, including its physical quantity, provenance, SI range, accuracy, and update frequency; no live value is stored.";
        if (type.Name == "MetaInfo") return "Shared resource metadata containing the caller-owned UUID and optional HTTP location fields.";
        if (type == typeof(RigMast)) return "Rig mast assembly containing hoisting, rotary, pipe-handling, standpipe, choke, and related equipment.";
        if (typeof(RigEquipmentBase).IsAssignableFrom(type)) return $"{SplitName(type.Name)} equipment definition, including identity and manufacturer details plus its type-specific ratings, limits, and optional instrumentation capabilities.";
        if (typeof(RigComponentBase).IsAssignableFrom(type)) return $"{SplitName(type.Name)} component definition with a name, description, and any component-specific fields.";
        return $"Nested {SplitName(type.Name)} definition used by the Rig model.";
    }

    private static string DescribeProperty(PropertyInfo property)
    {
        if (PropertyDescriptions.TryGetValue(property.Name, out string? exact)) return exact;
        Type type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
        string label = SplitName(property.Name);
        Type? itemType = CollectionItemType(type);
        if (itemType is not null) return $"Collection of {SplitName(itemType.Name)} definitions. Send full nested objects, not resource UUIDs.";
        if (type.IsEnum) return $"{label} classification. Use one of the exact string values listed by this schema.";
        if (type == typeof(bool)) return $"Whether {label.ToLowerInvariant()} is enabled or present.";
        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal)) return NumericDescription(property, label);
        if (!type.IsValueType && type != typeof(string)) return $"Optional nested {label} definition. Set null or omit it when the rig does not have this component.";
        return $"{label} value for this rig component.";
    }

    private static string NumericDescription(PropertyInfo property, string label)
    {
        string propertyKey = $"{property.DeclaringType?.Name}.{property.Name}";
        if (ScalarUnits.TryGetValue(propertyKey, out string? scalarUnit))
        {
            return $"{label} in {scalarUnit}; do not send a display-unit value.";
        }

        string name = property.Name;
        string key = name.ToLowerInvariant();
        string? unit = key switch
        {
            _ when key.Contains("temperature") => "kelvin (K)",
            _ when key.Contains("pressure") || key.Contains("shearstress") => "pascal (Pa)",
            _ when key.Contains("torque") || key.EndsWith("trq") => "newton metre (N·m)",
            _ when key.Contains("power") => "watt (W)",
            _ when key.Contains("density") || key.Contains("mudweight") => "kilogram per cubic metre (kg/m³)",
            _ when key.Contains("flow") || key.Contains("pumprate") => "cubic metre per second (m³/s)",
            _ when key.Contains("volume") => "cubic metre (m³)",
            _ when key.Contains("angle") || key.Contains("orientation") || key.Contains("azimuth") => "radian (rad)",
            _ when key.Contains("rotation") || key.Contains("angular") || key.Contains("frequency") || key.Contains("strokera") => "radian per second (rad/s)",
            _ when key.Contains("acceleration") => "metre per second squared (m/s²)",
            _ when key.Contains("velocity") || key.Contains("windspeed") || key.Contains("coilspeed") || key.Contains("maxspeed") => "metre per second (m/s)",
            _ when key.Contains("time") || key.Contains("batterylife") => "second (s)",
            _ when key.Contains("diameter") || key.EndsWith("od") || key.EndsWith("id") || key.Contains("radius") || key.Contains("height") || key.Contains("length") || key.Contains("elevation") || key.Contains("position") || key.Contains("clearance") || key.Contains("stroke") => "metre (m)",
            _ when key.Contains("mass") => "kilogram (kg)",
            _ when key.Contains("load") || key.Contains("hook") || key.Contains("tension") || key.Contains("force") || key == "weight" => "newton (N)",
            _ when key.Contains("efficiency") || key.Contains("factor") || key.Contains("gain") || key.Contains("cvvalue") || key.Contains("clogging") => "dimensionless SI ratio",
            _ => null
        };
        return unit is null
            ? $"{label} numeric value in the SI unit appropriate to this equipment property; do not send a display-unit value."
            : $"{label} in {unit}; do not send a display-unit value.";
    }

    private static string SplitName(string value) => System.Text.RegularExpressions.Regex.Replace(value, "(?<=[a-z0-9])([A-Z])", " $1");

    public static bool TryParseGuid(JsonObject? arguments, string key, out Guid value, out JsonNode? error)
    {
        value = Guid.Empty;
        error = null;
        if (arguments?[key] is null)
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' is required.");
            return false;
        }
        if (!Guid.TryParse(arguments[key]!.ToString(), out value))
        {
            error = McpToolResponses.CreateValidationError($"Argument '{key}' must be a valid UUID.");
            return false;
        }
        return true;
    }
}
