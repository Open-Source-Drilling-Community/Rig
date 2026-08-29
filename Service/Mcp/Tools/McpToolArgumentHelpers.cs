using System.Collections;
using System.Reflection;
using System.Text.Json.Nodes;
using NORCE.Drilling.Rig.Model;

namespace NORCE.Drilling.Rig.Service.Mcp.Tools;

internal static class McpToolArgumentHelpers
{
    private static readonly IReadOnlyDictionary<string, string> PropertyDescriptions =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MetaInfo"] = "Resource metadata. MetaInfo.ID is the persistent rig UUID and is supplied by the caller.",
            ["Name"] = "Human-readable name of the rig, mast, component, or equipment item.",
            ["Description"] = "Human-readable description of the rig, component, capabilities, or intended use.",
            ["CreationDate"] = "Creation timestamp in ISO 8601 format. Use a UTC offset where possible.",
            ["LastModificationDate"] = "Last-modification timestamp in ISO 8601 format. Update this when replacing the rig.",
            ["IsFixedPlatform"] = "Whether this is a fixed-platform rig. When true, ClusterID should identify its Cluster; when false, ClusterID should be null.",
            ["ClusterID"] = "UUID of the Cluster hosting a fixed-platform rig. This is an external reference to the Cluster microservice, not an embedded Cluster object; leave null for non-fixed rigs.",
            ["DrillFloorElevation"] = "Drill-floor elevation in metres (m). The Rig payload stores only the SI scalar and no vertical-datum identifier, so callers must apply the configured depth-reference convention consistently.",
            ["MainRigMast"] = "Primary rig-mast assembly and its hoisting, rotary, pipe-handling, standpipe, choke, and related equipment.",
            ["AuxiliaryRigMast"] = "Optional secondary rig-mast assembly with the same nested equipment structure as MainRigMast.",
            ["MudPumpList"] = "Mud-circulation pumps installed on the rig, including equipment identity, pump class, displacement curve, and operating limits.",
            ["CementPumpList"] = "Cement pumps installed on the rig, including displacement curve and pressure/flow limits.",
            ["MudTankList"] = "Mud tanks and their class, fluid type, capacities, and operating measurements.",
            ["GeneratorList"] = "Electrical generators and their engine, cooling, phase, speed, power, and efficiency characteristics.",
            ["ShaleShakerList"] = "Shale shakers and their classification, active state, screen definitions, and capacity.",
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

    public static JsonObject CreateRigSchema(bool includeId = false)
    {
        var definitions = new JsonObject();
        JsonObject rigSchema = TypeSchema(typeof(Model.Rig), definitions, nullable: false);
        rigSchema["description"] = "Complete Rig representation. JSON property names are case-sensitive and use PascalCase. Optional equipment may be null or omitted; supplied measurements and limits use SI values.";

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
        if (type.Name == "MetaInfo") return "Shared resource metadata containing the caller-owned UUID and optional HTTP location fields.";
        if (type == typeof(RigMast)) return "Rig mast assembly containing hoisting, rotary, pipe-handling, standpipe, choke, and related equipment.";
        if (typeof(RigEquipmentBase).IsAssignableFrom(type)) return $"{SplitName(type.Name)} equipment definition, including identity and manufacturer details plus its type-specific ratings, limits, and measurements.";
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
        if (type == typeof(double) || type == typeof(float) || type == typeof(decimal)) return NumericDescription(property.Name, label);
        if (!type.IsValueType && type != typeof(string)) return $"Optional nested {label} definition. Set null or omit it when the rig does not have this component.";
        return $"{label} value for this rig component.";
    }

    private static string NumericDescription(string name, string label)
    {
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
