using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Rig.Model;

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class RigDiscoveryPage
{
    public List<RigLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}

/// <summary>Discovery projection; excludes engineering arrays and nested definitions.</summary>
public sealed class RigFeatureCategoryLight
{
    public OSDC.DotnetLibraries.General.DataManagement.MetaInfo? MetaInfo { get; set; }
    public string? Code { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public bool IsExclusive { get; set; }
    public bool HasValidityPeriod { get; set; }
    public bool IsBuiltIn { get; set; }
    public bool IsDeprecated { get; set; }
    public System.DateTimeOffset? CreationDate { get; set; }
    public System.DateTimeOffset? LastModificationDate { get; set; }
}

/// <summary>A bounded UUID-ordered page. NextOffset is null on the final page.</summary>
public sealed class RigFeatureCategoryDiscoveryPage
{
    public List<RigFeatureCategoryLight> Items { get; set; } = new();
    public int Offset { get; set; }
    public int Limit { get; set; }
    public int TotalCount { get; set; }
    public int? NextOffset { get; set; }
}
