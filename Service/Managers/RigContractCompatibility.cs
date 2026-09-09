using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using OSDC.Drilling.Rig.Model;

namespace OSDC.Drilling.Rig.Service.Managers;

internal static class RigContractCompatibility
{
    internal const double DefaultDrillFloorDepthStandardDeviation = 0.5;

    /// <summary>
    /// Expands either compatibility representation into both representations.
    /// Remove this with DrillFloorElevation and IsFixedPlatform after all
    /// consumers and durable data have migrated.
    /// </summary>
    internal static List<string> Normalize(Model.Rig rig)
    {
        List<string> errors = [];

#pragma warning disable CS0618 // Compatibility fields are intentional during the expand phase.
        if ((rig.RigType is null or RigType.Unknown) && rig.IsFixedPlatform)
            rig.RigType = RigType.PlatformRig;

        bool isPlatformRig = rig.RigType == RigType.PlatformRig;
        rig.IsFixedPlatform = isPlatformRig;

        GaussianDrillingProperty? depth = rig.FixedPlatformProperties?.DrillFloorDepth;
        if (!isPlatformRig)
        {
            if (rig.FixedPlatformProperties is not null)
                errors.Add("FixedPlatformProperties is allowed only when RigType is PlatformRig.");
            return errors;
        }

        if (depth is null && rig.DrillFloorElevation is double legacyDepth)
        {
            depth = new GaussianDrillingProperty
            {
                Mean = legacyDepth,
                StandardDeviation = DefaultDrillFloorDepthStandardDeviation
            };
            rig.FixedPlatformProperties = new FixedPlatformProperties { DrillFloorDepth = depth };
        }
        else if (depth is not null)
        {
            if (depth.Mean is null && rig.DrillFloorElevation is double legacyDepthForMean)
                depth.Mean = legacyDepthForMean;
            if (depth.Mean is not null && depth.StandardDeviation is null)
                depth.StandardDeviation = DefaultDrillFloorDepthStandardDeviation;

            if (rig.DrillFloorElevation is null && depth.Mean is double mean)
                rig.DrillFloorElevation = mean;
            else if (rig.DrillFloorElevation is double oldValue && depth.Mean is double newValue && oldValue != newValue)
                errors.Add("DrillFloorElevation and FixedPlatformProperties.DrillFloorDepth.Mean must match during the compatibility period.");
        }
#pragma warning restore CS0618

        return errors;
    }
}
