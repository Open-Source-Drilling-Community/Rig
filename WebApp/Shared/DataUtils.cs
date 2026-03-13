using System;
using System.Collections.Generic;
using NORCE.Drilling.Rig.ModelShared;

public static class DataUtils
{
    public const string DefaultRigName = "New rig";
    public const string DefaultRigDescription = "Rig description";

    public static class UnitAndReferenceParameters
    {
        public static string? UnitSystemName { get; set; } = "Metric";
        public static string? DepthReferenceName { get; set; }
        public static string? PositionReferenceName { get; set; }
        public static string? AzimuthReferenceName { get; set; }
        public static string? PressureReferenceName { get; set; }
        public static string? DateReferenceName { get; set; }
    }

    public static void UpdateUnitSystemName(string value) => UnitAndReferenceParameters.UnitSystemName = value;

    public static Rig CreateDefaultRig()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        return new Rig
        {
            MetaInfo = new MetaInfo
            {
                ID = Guid.NewGuid(),
                HttpHostName = APIUtils.HostNameRig,
                HttpHostBasePath = APIUtils.HostBasePathRig,
                HttpEndPoint = "Rig/"
            },
            Name = DefaultRigName,
            Description = DefaultRigDescription,
            CreationDate = now,
            LastModificationDate = now,
            MainRigMast = new RigMast
            {
                Name = "Main Rig Mast",
                StandPipe = new StandPipe
                {
                    Name = "Stand Pipe",
                    PressureMeasurementElevation = null,
                    MudHoseHangingPointElevation = null
                },
                StandPipeManifold = new StandPipeManifold { Name = "Stand Pipe Manifold" },
                CatWalk = new CatWalk { Name = "Cat Walk" },
                RotaryTable = new RotaryTable { Name = "Rotary Table" },
                ChokeManifold = new ChokeManifold { Name = "Choke Manifold" }
            },
            MudPumpList = new List<MudPump>
            {
                new MudPump
                {
                    Name = "Mud Pump 1"
                },
                new MudPump
                {
                    Name = "Mud Pump 2"
                }
            },
            ShaleShakerList = new List<ShaleShaker>
            {
                new ShaleShaker
                {
                    Name = "Shale Shaker 1"
                }
            },
            ReturnFlowLine = new ReturnFlowLine
            {
                Name = "Return Flow Line"
            },
            MudTankList = new List<MudTank>
            {
                new MudTank
                {
                    Name = "Active Tank",
                    TankClass = TankClass.Active,
                    TankFluidType = TankFluidType.DrillingMud
                },
                new MudTank
                {
                    Name = "Reserve Tank",
                    TankClass = TankClass.Reserve,
                    TankFluidType = TankFluidType.DrillingMud
                },
                new MudTank
                {
                    Name = "Slug Tank",
                    TankClass = TankClass.Slug,
                    TankFluidType = TankFluidType.DrillingMud
                },
                new MudTank
                {
                    Name = "Trip Tank",
                    TankClass = TankClass.Trip,
                    TankFluidType = TankFluidType.DrillingMud
                }
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

    public static string InferQuantity(string propertyName)
    {
        string key = propertyName.ToLowerInvariant();
        if (key == "pressuremeasurementelevation" || key == "mudhosehangingpointelevation") return "LengthStandard";
        if (key == "tableopeningdiameter" || key == "bushingsize") return "DiameterPipeDrilling";
        if (key == "height") return "LengthStandard";
        if (key == "mass") return "MassDrilling";
        if (key == "maxlimitoperatingtorque" || key == "maxlimitdesigntorque") return "TorqueDrilling";
        if (key == "maxlimitoperatingspeed" || key == "maxlimitdesignspeed") return "AngularVelocityDrilling";
        if (key == "maxlimitoperatingstringweight" || key == "maxlimitdesignstringweight") return "HookLoadDrilling";
        if (key == "maxlimitoperatingload" || key == "maxlimitdesignload") return "HookLoadDrilling";
        if (key == "maxlimitcontinuousdrumtorque") return "TorqueDrilling";
        if (key == "maxlimitcontinuousdrumpower") return "PowerDrilling";
        if (key == "maxlimittemperature") return "TemperatureDrilling";
        if (key == "maxlimitpower") return "PowerDrilling";
        if (key == "linerid") return "DiameterPipeDrilling";
        if (key == "pumpdisplacement") return "VolumeDrilling";
        if (key == "stroke") return "LengthStandard";
        if (key.Contains("efficiency")) return "ProportionStandard";
        if (key.Contains("pressure")) return "PressureDrilling";
        if (key.Contains("flowrate")) return "VolumetricFlowrateDrilling";
        if (key.Contains("volume") || key.Contains("capacity")) return "VolumeDrilling";
        if (key.Contains("speed") || key.Contains("strokera")) return "AngularVelocityDrilling";
        if (key.Contains("power")) return "PowerDrilling";
        if (key.Contains("torque")) return "Torque";
        if (key.Contains("density")) return "MassDensity";
        if (key.Contains("flow") || key.Contains("rate")) return "VolumetricFlowrateDrilling";
        if (key.Contains("frequency")) return "AngularVelocityDrilling";
        if (key.Contains("force") || key.Contains("load") || key.Contains("hook") || key.Contains("tension") || key.Contains("weight")) return "Force";
        if (key.Contains("angle") || key.Contains("orientation") || key.Contains("azimuth")) return "PlaneAngle";
        if (key.Contains("time")) return "Time";
        if (key.Contains("temperature")) return "TemperatureDrilling";
        if (key.Contains("diameter")) return "DiameterPipeDrilling";
        if (key.Contains("radius") || key.Contains("height") || key.Contains("length") || key.Contains("depth") || key.Contains("elevation") || key.Contains("position") || key.Contains("clearance")) return "Length";
        return "Dimensionless";
    }
}
