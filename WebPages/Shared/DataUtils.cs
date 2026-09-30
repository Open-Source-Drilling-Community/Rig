using OSDC.UnitConversion.DrillingRazorMudComponents;
using OSDC.Drilling.Rig.WebPages.Semantics;
using RigModel = OSDC.Drilling.Rig.ModelShared;

namespace OSDC.Drilling.Rig.WebPages.Shared;

public static class DataUtils
{
    public const string DefaultRigName = "New rig";
    public const string DefaultRigDescription = "Rig description";

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
            if (rig.RigType == RigModel.RigType.PlatformRig &&
                rig.FixedPlatformProperties?.DrillFloorDepth?.GaussianValue?.Mean is double drillFloorDepth)
            {
                UnitAndReferenceParameters.RotaryTableDepthReferenceSource.RotaryTableDepthReference = -drillFloorDepth;
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

    public static string InferQuantity(Type declaringType, string propertyName) =>
        ProviderSemantics.QuantityName(declaringType, propertyName);

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
