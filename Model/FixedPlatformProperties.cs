using DWIS.API.DTO;
using DWIS.Vocabulary.Schemas;
using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using OSDC.UnitConversion.Conversion;
using OSDC.UnitConversion.Conversion.DrillingEngineering;

namespace OSDC.Drilling.Rig.Model;

/// <summary>
/// Properties that are valid only when <see cref="Rig.RigType"/> is
/// <see cref="Model.RigType.PlatformRig"/>.
/// </summary>
public sealed class FixedPlatformProperties
{
    /// <summary>
    /// Vertical depth of the drill floor in SI metres relative to WGS84.
    /// Migrated historical values use this depth convention without sign inversion.
    /// </summary>
    [AccessToVariable(CommonProperty.VariableAccessType.Assignable)]
    [Mandatory(CommonProperty.MandatoryType.General)]
    [SemanticGaussianVariable("drill_floor_depth_rig", "sigma_drill_floor_depth_rig")]
    [SemanticFact("drill_floor_depth_rig", Nouns.Enum.DrillingSignal)]
    [SemanticFact("drill_floor_depth_rig#01", Nouns.Enum.PhysicalData)]
    [SemanticFact("drill_floor_depth_rig#01", Nouns.Enum.ContinuousDataType)]
    [SemanticFact("drill_floor_depth_rig#01", Verbs.Enum.HasDynamicValue, "drill_floor_depth_rig")]
    [SemanticFact("drill_floor_depth_rig#01", Verbs.Enum.IsOfMeasurableQuantity, DrillingPhysicalQuantity.QuantityEnum.DepthDrilling)]
    [SemanticFact("MovingAverage", Nouns.Enum.MovingAverage)]
    [SemanticFact("drill_floor_depth_rig#01", Verbs.Enum.IsTransformationOutput, "MovingAverage")]
    [SemanticFact("sigma_drill_floor_depth_rig", Nouns.Enum.DrillingSignal)]
    [SemanticFact("sigma_drill_floor_depth_rig#01", Nouns.Enum.DrillingDataPoint)]
    [SemanticFact("sigma_drill_floor_depth_rig#01", Verbs.Enum.HasValue, "sigma_drill_floor_depth_rig")]
    [SemanticFact("GaussianUncertainty#01", Nouns.Enum.GaussianUncertainty)]
    [SemanticFact("drill_floor_depth_rig#01", Verbs.Enum.HasUncertainty, "GaussianUncertainty#01")]
    [SemanticFact("GaussianUncertainty#01", Verbs.Enum.HasUncertaintyStandardDeviation, "sigma_drill_floor_depth_rig#01")]
    [SemanticFact("GaussianUncertainty#01", Verbs.Enum.HasUncertaintyMean, "drill_floor_depth_rig#01")]
    [DefaultStandardDeviation(0.5)]
    public GaussianDrillingProperty? DrillFloorDepth { get; set; }
}
