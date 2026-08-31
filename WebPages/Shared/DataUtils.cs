using OSDC.UnitConversion.DrillingRazorMudComponents;
using RigModel = OSDC.Drilling.Rig.ModelShared;

namespace OSDC.Drilling.Rig.WebPages.Shared;

public static class DataUtils
{
    public const string DefaultRigName = "New rig";
    public const string DefaultRigDescription = "Rig description";

    private static readonly IReadOnlyDictionary<string, string> PropertyQuantities =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["AutoDriller.MaxLimitRop"] = "RateOfPenetrationDrilling",
            ["AutoDriller.MinLimitRop"] = "RateOfPenetrationDrilling",
            ["AutoDriller.MaxLimitWob"] = "WeightOnBitDrilling",
            ["AutoDriller.MinLimitWob"] = "WeightOnBitDrilling",
            ["AutoDriller.MaxLimitTrq"] = "TorqueDrilling",
            ["AutoDriller.MinLimitTrq"] = "TorqueDrilling",
            ["BopLineDefinition.LineId"] = "DiameterPipeDrilling",
            ["BopLineDefinition.LineOd"] = "DiameterPipeDrilling",
            ["CasingDriveSystem.HoistingCapacity"] = "HookLoadDrilling",
            ["CasingDriveSystem.MaxLimitPushDown"] = "ForceDrilling",
            ["CoilDriveSystem.ReelPayloadCapacity"] = "ForceDrilling",
            ["CoilDriveSystem.ReelPayloadLength"] = "LengthStandard",
            ["CoilDriveSystem.InjectorHeadMinTubingOd"] = "DiameterPipeDrilling",
            ["CoilDriveSystem.InjHeadDesignPullCapacity"] = "ForceDrilling",
            ["CoilDriveSystem.InjHeadDesignSnubCapacity"] = "ForceDrilling",
            ["CoilDriveSystem.InjHeadPullCapacity"] = "ForceDrilling",
            ["CoilDriveSystem.InjHeadSnubCapacity"] = "ForceDrilling",
            ["CoilDriveSystem.InjHeadMaxSpeed"] = "AxialVelocityDrilling",
            ["ContinuousCirculationDevice.MaxLimitMudWeight"] = "MassDensityDrilling",
            ["ContinuousCirculationDevice.MaxLimitRotationRate"] = "AngularVelocityDrilling",
            ["CrownBlock.GrooveDiameter"] = "CableDiameterDrilling",
            ["CrownBlock.MaxLimitCompensatorStroke"] = "LengthStandard",
            ["Derrick.MaxLimitWindSpeed"] = "Velocity",
            ["DrillingChokeManifold.MaxLimitOpeningSpeed"] = "ChokeOpeningRateDrilling",
            ["DrillingChokeManifold.TrimSize"] = "DiameterPipeDrilling",
            ["DrillingChokeManifold.FlowMeterSize"] = "DiameterPipeDrilling",
            ["DrillingMarineRiser.JointWeight"] = "MassGradientPerLengthDrilling",
            ["DrillLine.Diameter"] = "CableDiameterDrilling",
            ["DrillLine.LinearWeight"] = "MassGradientPerLengthDrilling",
            ["DrillstringHeaveCompensator.MaxLimitCompensatorStroke"] = "LengthStandard",
            ["FlowRoutingManifold.FlangeSize"] = "DiameterPipeDrilling",
            ["FlowRoutingManifold.PressureReliefValveTrim"] = "DiameterPipeDrilling",
            ["Generator.PowerFactor"] = "ProportionStandard",
            ["Generator.StartupTimeCold"] = "DurationDrilling",
            ["Generator.StartupTimeWarm"] = "DurationDrilling",
            ["Generator.Voltage"] = "ElectricTension",
            ["Generator.MaxLimitVoltage"] = "ElectricTension",
            ["Generator.MinLimitVoltage"] = "ElectricTension",
            ["Generator.MaxLimitPowerIncrease"] = "PowerRateOfChangeDrilling",
            ["Generator.MaxLimitSpeedIncrease"] = "RotationalFrequencyRateOfChangeDrilling",
            ["Generator.MaxLimitFrequency"] = "Frequency",
            ["Generator.MinLimitFrequency"] = "Frequency",
            ["MarineMpdEquipment.Weight"] = "MassDrilling",
            ["MarineUnitProfile.MaximumTransitSpeed"] = "Velocity",
            ["MeasurementAfm.UpdateRate"] = "Frequency",
            ["MpdControlDevice.NominalSize"] = "DiameterPipeDrilling",
            ["MpdController.PrimaryChokeTrim"] = "DiameterPipeDrilling",
            ["MpdController.SecondaryChokeTrim"] = "DiameterPipeDrilling",
            ["MudPump.MaxLimitOperatingSpeed"] = "StrokeFrequency",
            ["CementPumpDisplacementPoint.StrokeRate"] = "StrokeFrequency",
            ["Rig.DrillFloorElevation"] = "HeightDrilling",
            ["RigOperatingEnvelope.MaximumDrillingDepth"] = "DepthDrilling",
            ["RigOperatingEnvelope.MaximumWaterDepth"] = "DepthDrilling",
            ["RigOperatingEnvelope.MaximumOperatingWindSpeed"] = "Velocity",
            ["RigOperatingEnvelope.MaximumSurvivalWindSpeed"] = "Velocity",
            ["RiserHeaveCompensator.MaxLimitCompensatorStroke"] = "LengthStandard",
            ["ShaleShaker.MaxLimitOperatingCapacity"] = "VolumetricFlowrateDrilling",
            ["SurfaceMpdEquipment.MinimumBoreholeSize"] = "DiameterPipeDrilling",
            ["SurfaceMpdEquipment.MaximumBoreholeSize"] = "DiameterPipeDrilling",
            ["SurfaceMpdEquipment.MaxLimitMudWeight"] = "MassDensityDrilling",
            ["TopDrive.Weight"] = "MassDrilling",
            ["TopDrive.TorqueHighPassFilterTimeConstant"] = "DurationDrilling",
            ["TopDrive.TorqueLowPassFilterTimeConstant"] = "DurationDrilling",
            ["TopDrive.VFDFilterTimeConstant"] = "DurationDrilling",
            ["TopDrive.EncoderTimeConstant"] = "DurationDrilling",
            ["TopDrive.AccelerationFilterTimeConstant"] = "DurationDrilling",
            ["TorqueTurnSub.Weight"] = "MassDrilling",
            ["TorqueTurnSub.BatteryLife"] = "DurationDrilling",
            ["TravellingBlock.GrooveDiameter"] = "CableDiameterDrilling",
            ["TravellingBlock.MaxLimitBlockTravel"] = "LengthStandard",
            ["EquipmentMeasurementCapability.RelativeAccuracy"] = "ProportionStandard",
            ["EquipmentMeasurementCapability.UpdateFrequency"] = "Frequency"
        };

    public static class UnitAndReferenceParameters
    {
        public static string? UnitSystemName { get; set; } = "Metric";
        public static string? DepthReferenceName { get; set; } = "WGS84";
        public static string? PositionReferenceName { get; set; }
        public static string? AzimuthReferenceName { get; set; }
        public static string? PressureReferenceName { get; set; }
        public static string? DateReferenceName { get; set; }
        public static GroundMudLineDepthReferenceSource GroundMudLineDepthReferenceSource { get; set; } = new();
        public static MeanSeaLevelDepthReferenceSource MeanSeaLevelDepthReferenceSource { get; set; } = new();
        public static RotaryTableDepthReferenceSource RotaryTableDepthReferenceSource { get; set; } = new();
        public static SeaWaterLevelDepthReferenceSource SeaWaterLevelDepthReferenceSource { get; set; } = new();
    }

    public static void ApplyRigReferenceValues(RigModel.Rig? rig, Dictionary<Guid, RigModel.Cluster> clusters)
    {
        if (rig != null && rig.ClusterID != null)
        {
            RigModel.Cluster? cluster = null;
            if (clusters.TryGetValue(rig.ClusterID.Value, out cluster))
            {
                ApplyRigClusterReferenceValues(rig, cluster);
                return;
            }
        }
        ApplyRigClusterReferenceValues(rig, null);
    }

    public static void ApplyRigClusterReferenceValues(RigModel.Rig? rig, RigModel.Cluster? cluster)
    {
        UnitAndReferenceParameters.GroundMudLineDepthReferenceSource.GroundMudLineDepthReference = 0;
        UnitAndReferenceParameters.MeanSeaLevelDepthReferenceSource.MeanSeaLevelDepthReference = null;
        UnitAndReferenceParameters.RotaryTableDepthReferenceSource.RotaryTableDepthReference = 0;
        UnitAndReferenceParameters.SeaWaterLevelDepthReferenceSource.SeaWaterLevelDepthReference = 0;
        if (rig != null)
        {
            if (rig.DrillFloorElevation != null)
            {
                UnitAndReferenceParameters.RotaryTableDepthReferenceSource.RotaryTableDepthReference = -rig.DrillFloorElevation;
            }
            if (cluster != null)
            {
                if (cluster.GroundMudLineDepth?.GaussianValue?.Mean != null)
                {
                    UnitAndReferenceParameters.GroundMudLineDepthReferenceSource.GroundMudLineDepthReference = -cluster.GroundMudLineDepth.GaussianValue.Mean;
                }
                if (cluster.TopWaterDepth?.GaussianValue?.Mean != null)
                {
                    UnitAndReferenceParameters.SeaWaterLevelDepthReferenceSource.SeaWaterLevelDepthReference = -cluster.TopWaterDepth.GaussianValue.Mean;
                }
            }
        }
    }

    public static void UpdateUnitSystemName(string value) => UnitAndReferenceParameters.UnitSystemName = value;
    public static void UpdateDepthReferenceName(string value) => UnitAndReferenceParameters.DepthReferenceName = value;

    public static RigModel.Rig CreateDefaultRig(IRigAPIUtils api)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new RigModel.Rig
        {
            MetaInfo = new RigModel.MetaInfo
            {
                ID = Guid.NewGuid(),
                HttpHostName = api.HostNameRig,
                HttpHostBasePath = api.HostBasePathRig,
                HttpEndPoint = "Rig/"
            },
            Name = DefaultRigName,
            Description = DefaultRigDescription,
            CreationDate = now,
            LastModificationDate = now,
            MainRigMast = new RigModel.RigMast
            {
                Name = "Main Rig Mast",
                StandPipe = new RigModel.StandPipe
                {
                    Name = "Stand Pipe",
                    PressureMeasurementElevation = null,
                    MudHoseHangingPointElevation = null
                },
                StandPipeManifold = new RigModel.StandPipeManifold { Name = "Stand Pipe Manifold" },
                CatWalk = new RigModel.CatWalk { Name = "Cat Walk" },
                RotaryTable = new RigModel.RotaryTable { Name = "Rotary Table" },
                ChokeManifold = new RigModel.ChokeManifold { Name = "Choke Manifold" }
            },
            MudPumpList = new List<RigModel.MudPump>
            {
                new() { Name = "Mud Pump 1" },
                new() { Name = "Mud Pump 2" }
            },
            ShaleShakerList = new List<RigModel.ShaleShaker>
            {
                new() { Name = "Shale Shaker 1" }
            },
            ReturnFlowLine = new RigModel.ReturnFlowLine
            {
                Name = "Return Flow Line"
            },
            MudTankList = new List<RigModel.MudTank>
            {
                new() { Name = "Active Tank", TankClass = RigModel.TankClass.Active, TankFluidType = RigModel.TankFluidType.DrillingMud },
                new() { Name = "Reserve Tank", TankClass = RigModel.TankClass.Reserve, TankFluidType = RigModel.TankFluidType.DrillingMud },
                new() { Name = "Slug Tank", TankClass = RigModel.TankClass.Slug, TankFluidType = RigModel.TankFluidType.DrillingMud },
                new() { Name = "Trip Tank", TankClass = RigModel.TankClass.Trip, TankFluidType = RigModel.TankFluidType.DrillingMud }
            }
        };
    }

    public static string GetDisplayName(string propertyName)
    {
        if (string.IsNullOrWhiteSpace(propertyName))
        {
            return string.Empty;
        }

        List<char> chars = new(propertyName.Length + 8) { propertyName[0] };
        for (int i = 1; i < propertyName.Length; i++)
        {
            char current = propertyName[i];
            char previous = propertyName[i - 1];
            if (char.IsUpper(current) && !char.IsUpper(previous))
            {
                chars.Add(' ');
            }
            chars.Add(current);
        }
        return new string(chars.ToArray());
    }

    public static string InferQuantity(Type declaringType, string propertyName)
    {
        if (PropertyQuantities.TryGetValue($"{declaringType.Name}.{propertyName}", out string? quantity))
        {
            return quantity;
        }

        string key = propertyName.ToLowerInvariant();
        if (key == "pressuremeasurementelevation" || key == "mudhosehangingpointelevation") return "LengthStandard";
        if (key == "tableopeningdiameter" || key == "bushingsize") return "DiameterPipeDrilling";
        if (key == "height") return "LengthStandard";
        if (key == "mass") return "MassDrilling";
        if (key == "operatingdisplacement" || key == "maximummass") return "MassDrilling";
        if (key == "ratedhoistingcapacity" || key == "ratedhookload" || key == "maximumsetbackload" || key == "maximumrotaryload" || key == "maximumpreload" || key == "maximummooringlinetension") return "HookLoadDrilling";
        if (key == "ratedcontinuoustorque" || key == "ratedintermittenttorque") return "TorqueDrilling";
        if (key == "maximumvolume") return "VolumeDrilling";
        if (key == "hulllength" || key == "hullwidth" || key == "hulldepth" || key == "operatingdraft" || key == "transitdraft" || key == "leglength" || key == "longitudinallegspacing" || key == "transverselegspacing" || key == "maximumcantileverskidout" || key == "maximumcantilevertransversereach" || key == "substructuretravel") return "LengthStandard";
        if (key == "maxlimitoperatingtorque" || key == "maxlimitdesigntorque") return "TorqueDrilling";
        if (key == "maxlimitoperatingspeed" || key == "maxlimitdesignspeed") return "AngularVelocityDrilling";
        if (key == "maxlimitoperatingstringweight" || key == "maxlimitdesignstringweight") return "HookLoadDrilling";
        if (key == "maxlimitoperatingload" || key == "maxlimitdesignload") return "HookLoadDrilling";
        if (key == "maxlimitcontinuousdrumtorque") return "TorqueDrilling";
        if (key == "maxlimitcontinuousdrumpower") return "PowerDrilling";
        if (key == "maxlimittemperature") return "TemperatureDrilling";
        if (key == "maxlimitpower") return "PowerDrilling";
        if (key == "linerinnerdiameter") return "DiameterPipeDrilling";
        if (key == "displacementperstroke") return "VolumeDrilling";
        if (key == "stroke") return "LengthStandard";
        if (key.Contains("efficiency")) return "ProportionStandard";
        if (key.Contains("pressure")) return "PressureDrilling";
        if (key.Contains("flowrate")) return "VolumetricFlowrateDrilling";
        if (key.Contains("volume") || key.Contains("capacity")) return "VolumeDrilling";
        if (key.Contains("speed") || key.Contains("strokera")) return "AngularVelocityDrilling";
        if (key.Contains("power")) return "PowerDrilling";
        if (key.Contains("torque") || key.EndsWith("trq")) return "TorqueDrilling";
        if (key.Contains("density")) return "MassDensityDrilling";
        if (key.Contains("flow") || key.Contains("rate")) return "VolumetricFlowrateDrilling";
        if (key.Contains("frequency")) return "AngularVelocityDrilling";
        if (key.Contains("force") || key.Contains("load") || key.Contains("hook") || key.Contains("tension") || key.Contains("weight")) return "Force";
        if (key.Contains("angle") || key.Contains("orientation") || key.Contains("azimuth")) return "PlaneAngle";
        if (key.Contains("time")) return "DurationDrilling";
        if (key.Contains("temperature")) return "TemperatureDrilling";
        if (key.Contains("diameter")) return "DiameterPipeDrilling";
        if (key.Contains("radius") || key.Contains("height") || key.Contains("length") || key.Contains("depth") || key.Contains("elevation") || key.Contains("position") || key.Contains("clearance")) return "Length";
        return "Dimensionless";
    }

    public class GroundMudLineDepthReferenceSource : IGroundMudLineDepthReferenceSource
    {
        public double? GroundMudLineDepthReference { get; set; }
    }

    public class MeanSeaLevelDepthReferenceSource : IMeanSeaLevelDepthReferenceSource
    {
        public double? MeanSeaLevelDepthReference { get; set; }
    }

    public class RotaryTableDepthReferenceSource : IRotaryTableDepthReferenceSource
    {
        public double? RotaryTableDepthReference { get; set; }
    }

    public class SeaWaterLevelDepthReferenceSource : ISeaWaterLevelDepthReferenceSource
    {
        public double? SeaWaterLevelDepthReference { get; set; }
    }
}
