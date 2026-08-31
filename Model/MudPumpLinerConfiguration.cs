namespace OSDC.Drilling.Rig.Model;

/// <summary>
/// Rated hydraulic performance for one liner size that can be installed in a mud pump.
/// All values use SI units: metres, cubic metres, cubic metres per second, and pascals.
/// </summary>
public class MudPumpLinerConfiguration
{
    /// <summary>Nominal inner diameter of the liner in metres.</summary>
    public double? LinerInnerDiameter { get; set; }

    /// <summary>Theoretical or manufacturer-rated displaced volume per pump stroke in cubic metres.</summary>
    public double? DisplacementPerStroke { get; set; }

    /// <summary>Maximum rated volumetric output for this liner at the pump's rated operating speed, in cubic metres per second.</summary>
    public double? MaximumVolumetricFlowRate { get; set; }

    /// <summary>Maximum rated discharge pressure for this liner in pascals.</summary>
    public double? MaximumDischargePressure { get; set; }
}
