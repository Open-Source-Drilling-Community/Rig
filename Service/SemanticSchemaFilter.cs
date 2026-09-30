using System.Collections.Generic;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.Drilling.Rig.Semantics;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OSDC.Drilling.Rig.Service;

/// <summary>Publishes the same reviewed Rig bindings used by MCP and WebPages.</summary>
public sealed class SemanticSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (ProviderSemantics.ForType(context.Type) is { } typeMetadata)
            schema.Extensions[SemanticMetadata.ExtensionName] = OpenApiAnyFactory.CreateFromJson(typeMetadata.ToJsonString());

        foreach (var property in context.Type.GetProperties())
        {
            if (!schema.Properties.TryGetValue(property.Name, out OpenApiSchema? target)) continue;
            var metadata = ProviderSemantics.ForProperty(property);
            var nested = ProviderSemantics.NestedBindings(property);
            if (metadata == null && nested == null) continue;

            // OpenAPI 3.0 ignores siblings of $ref, so preserve the reference through allOf.
            if (target.Reference != null)
            {
                target = new OpenApiSchema { AllOf = new List<OpenApiSchema> { target } };
                schema.Properties[property.Name] = target;
            }
            if (metadata != null)
                target.Extensions[SemanticMetadata.ExtensionName] = OpenApiAnyFactory.CreateFromJson(metadata.ToJsonString());
            if (nested != null)
                target.Extensions[ProviderSemantics.NestedBindingsExtension] = OpenApiAnyFactory.CreateFromJson(nested.ToJsonString());
        }
    }
}
