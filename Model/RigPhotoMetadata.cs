using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Rig.Model;

/// <summary>Lightweight descriptive information for a rig photograph. Image bytes are retrieved separately.</summary>
public class RigPhotoMetadata
{
    public MetaInfo? MetaInfo { get; set; }
    public Guid RigID { get; set; }
    public string? FileName { get; set; }
    public string? Title { get; set; }
    public string? Caption { get; set; }
    public string? AlternativeText { get; set; }
    public string? ContentType { get; set; }
    public long ByteLength { get; set; }
    public string? Sha256 { get; set; }
    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
    public string? Source { get; set; }
    public string? Attribution { get; set; }
    public string? License { get; set; }
    public DateTimeOffset? CreationDate { get; set; }
    public DateTimeOffset? LastModificationDate { get; set; }
}

/// <summary>Rig read response. Photos is omitted unless includePhotos=true is requested.</summary>
public class RigReadResponse : Rig
{
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull)]
    public List<RigPhotoMetadata>? Photos { get; set; }
}
