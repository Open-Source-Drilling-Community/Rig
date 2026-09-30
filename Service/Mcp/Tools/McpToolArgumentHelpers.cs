using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;
using OSDC.Drilling.Rig.Model;
using OSDC.Drilling.Rig.Semantics;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace OSDC.Drilling.Rig.Service.Mcp.Tools;

internal static class McpToolArgumentHelpers
{
    private static readonly NullabilityInfoContext Nullability = new();

    private static readonly IReadOnlyDictionary<string, string> PropertyDescriptions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MetaInfo"] = "Resource metadata. MetaInfo.ID is the persistent rig UUID and is supplied by the caller.",
            ["Name"] = "Human-readable name of the rig, mast, component, or equipment item.",
            ["Description"] = "Human-readable description of the rig, component, capabilities, or intended use.",
            ["CreationDate"] = "Creation timestamp in ISO 8601 format. Use a UTC offset where possible.",
            ["LastModificationDate"] = "Server-assigned last-modification timestamp in ISO 8601 format. Use the latest returned value as expectedModifiedUtc when replacing a stored rig.",
            ["RigType"] = "Authoritative rig classification and discriminator. FixedPlatformProperties is allowed only for the exact PlatformRig value.",
            ["ClusterID"] = "UUID of the Cluster hosting a PlatformRig. This is an external reference to the Cluster microservice, not an embedded Cluster object; leave null for every other RigType.",
            ["FixedPlatformProperties"] = "RigType-discriminated properties allowed only when RigType is PlatformRig.",
            ["DrillFloorDepth"] = "Gaussian drill-floor depth in SI metres relative to WGS84. GaussianValue.Mean is the depth and GaussianValue.StandardDeviation is its uncertainty; omitted historical uncertainty defaults to 0.5 m.",
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
        nullable |= underlying is not null;

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
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                         .Where(p => p.CanRead && p.CanWrite &&
                                     p.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() is null))
            {
                JsonObject propertySchema = TypeSchema(property.PropertyType, definitions, IsNullable(property));
                propertySchema["description"] = DescribeProperty(property);
                if (ProviderSemantics.ForProperty(property) is JsonObject metadata)
                    propertySchema[SemanticMetadata.ExtensionName] = metadata;
                if (ProviderSemantics.NestedBindings(property) is JsonObject nested)
                    propertySchema[ProviderSemantics.NestedBindingsExtension] = nested;
                properties[property.Name] = propertySchema;
            }

            var definition = new JsonObject
            {
                ["type"] = "object",
                ["description"] = DescribeType(type),
                ["properties"] = properties,
                ["additionalProperties"] = false
            };
            if (ProviderSemantics.ForType(type) is JsonObject typeMetadata)
                definition[SemanticMetadata.ExtensionName] = typeMetadata;
            if (type == typeof(Model.Rig))
            {
                definition["required"] = new JsonArray("MetaInfo");
                definition["allOf"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["if"] = new JsonObject
                        {
                            ["properties"] = new JsonObject { ["RigType"] = new JsonObject { ["const"] = nameof(RigType.PlatformRig) } },
                            ["required"] = new JsonArray("RigType")
                        },
                        ["else"] = new JsonObject
                        {
                            ["not"] = new JsonObject { ["required"] = new JsonArray("FixedPlatformProperties") }
                        }
                    }
                };
            }
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

    private static bool IsNullable(PropertyInfo property) =>
        Nullable.GetUnderlyingType(property.PropertyType) is not null ||
        (!property.PropertyType.IsValueType && Nullability.Create(property).ReadState is not NullabilityState.NotNull);

    private static string DescribeType(Type type)
    {
        if (type == typeof(Model.Rig)) return "Complete rig configuration containing identity, RigType-discriminated platform/Cluster association, Gaussian drill-floor depth, mast assemblies, and installed drilling equipment.";
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

    private static string NumericDescription(PropertyInfo property, string label) =>
        ProviderSemantics.NumericDescription(property, label);

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
