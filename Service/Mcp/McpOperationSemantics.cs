using System;
using System.Text.Json.Nodes;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;

namespace OSDC.Drilling.Rig.Service.Mcp;

internal static class McpOperationSemantics
{
    public static JsonNode Apply(string name, JsonNode schema)
    {
        string concept = name.StartsWith("rig_feature_category_", StringComparison.Ordinal) ? Concepts.FeatureCategory : Concepts.Rig;
        schema[SemanticMetadata.ExtensionName] = SemanticMetadata.Create(concept, Classify(name), assertionSource: "provider-mcp-operation");
        return schema;
    }
    private static string Classify(string name) =>
        name.Contains("batch_export", StringComparison.Ordinal) || name.Contains("get_all", StringComparison.Ordinal) ? Concepts.ResourceCollectionRetrieval
        : name.EndsWith("_get_by_id", StringComparison.Ordinal) ? Concepts.ResourceRetrieval
        : name.Contains("batch_restore", StringComparison.Ordinal) ? Concepts.ResourceOperation
        : name.EndsWith("_create", StringComparison.Ordinal) ? Concepts.ResourceCreation
        : name.EndsWith("_update_by_id", StringComparison.Ordinal) ? Concepts.ResourceReplacement
        : name.Contains("_delete", StringComparison.Ordinal) ? Concepts.ResourceDeletion : Concepts.ResourceOperation;
}
