using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace OSDC.Drilling.Rig.Model;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RigBatchExportScope { Unspecified = 0, All = 1, Selected = 2 }

public sealed class RigBatchExportRequest
{
    /// <summary>All exports every rig in UUID order. Selected preserves RigIDs order.</summary>
    public RigBatchExportScope Scope { get; set; }
    public List<Guid>? RigIDs { get; set; }
}

/// <summary>Portable, versioned backup of rigs, referenced feature definitions, Cluster names, and photos.</summary>
public sealed class RigBatchExportDocument
{
    public const string CurrentFormatIdentifier = "OSDC.Drilling.Rig.BatchExport";
    public const int CurrentSchemaVersion = 1;
    public string FormatIdentifier { get; set; } = CurrentFormatIdentifier;
    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public DateTimeOffset ExportedAtUtc { get; set; }
    public RigBatchCatalogDependencies CatalogDependencies { get; set; } = new();
    public RigBatchExternalReferences ExternalReferences { get; set; } = new();
    public List<Rig> Rigs { get; set; } = [];
    public List<RigBatchPhoto> Photos { get; set; } = [];
}

public sealed class RigBatchCatalogDependencies
{
    /// <summary>Only categories and options referenced by exported rigs.</summary>
    public List<RigFeatureCategory> FeatureCategories { get; set; } = [];
}

public sealed class RigBatchExternalReferences
{
    public List<RigBatchExternalReference> Clusters { get; set; } = [];
}

public sealed class RigBatchExternalReference
{
    public Guid SourceID { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class RigBatchPhoto
{
    public RigPhotoMetadata Metadata { get; set; } = new();
    /// <summary>Base64-encoded JPEG, PNG, or WebP content.</summary>
    public string ContentBase64 { get; set; } = string.Empty;
}

public sealed class RigBatchErrorEnvelope
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public List<RigBatchError> Errors { get; set; } = [];
}

public sealed class RigBatchError
{
    public int? PositionIndex { get; set; }
    public string Property { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RigBatchRestoreConflictPolicy { Unspecified = 0, FailIfExists = 1, ReplaceExisting = 2 }
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RigBatchCatalogRestorePolicy { Unspecified = 0, MapExisting = 1, MapOrCreateMissing = 2 }

public sealed class RigBatchRestoreRequest
{
    public RigBatchRestoreConflictPolicy ConflictPolicy { get; set; }
    public RigBatchCatalogRestorePolicy CatalogPolicy { get; set; }
    public RigBatchExportDocument? Document { get; set; }
}

public sealed class RigBatchRestoreResponse
{
    public DateTimeOffset RestoredAtUtc { get; set; }
    public int CreatedCount { get; set; }
    public int ReplacedCount { get; set; }
    public int RestoredPhotoCount { get; set; }
    public int CreatedCatalogDefinitionCount { get; set; }
    public int CreatedCatalogOptionCount { get; set; }
    public List<RigBatchCatalogMapping> CatalogMappings { get; set; } = [];
    public List<RigBatchExternalReferenceMapping> ExternalReferenceMappings { get; set; } = [];
    public List<Guid> RigIDs { get; set; } = [];
}

public sealed class RigBatchCatalogMapping
{
    public string Name { get; set; } = string.Empty;
    public Guid SourceID { get; set; }
    public Guid LocalID { get; set; }
    public string Resolution { get; set; } = string.Empty;
}

public sealed class RigBatchExternalReferenceMapping
{
    public string Resource { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Guid SourceID { get; set; }
    public Guid LocalID { get; set; }
    public string Resolution { get; set; } = string.Empty;
}
