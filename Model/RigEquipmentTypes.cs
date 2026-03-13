using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NORCE.Drilling.Rig.Model
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GeneratorClass
    {
        Diesel,
        Gas,
        Lng,
        ElectricGrid,
        Methanol,
        Hydrogen,
        Ammonia,
        Nuclear,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SpeedMode
    {
        ConstantSpeed,
        VariableSpeed
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum EngineModelType
    {
        Reciprocating,
        Turbine
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GeneratorCooling
    {
        Air,
        Water,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum GeneratorPhases
    {
        SinglePhase,
        TwoPhase,
        ThreePhase
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TopDriveClass
    {
        AcTopDrive,
        HydraulicTopDrive,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TopDriveControllerType
    {
        Unknown,
        StiffPIController,
        TunedPIController,
        ImpedanceMatching
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RotaryTableType
    {
        DcDrive,
        AcDrive,
        Hydraulic,
        Unknown
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RotaryTableBushingType
    {
        Master,
        Kelly,
        Unknown
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum KellyClass
    {
        Hexagonal,
        Square,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ControlClass
    {
        Manual,
        Automatic,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum PumpClass
    {
        DuplexPump,
        TriplexPump,
        QuintuplexPump,
        HexPump,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum StandpipeSpecLevel
    {
        Psl1LowPressureRating,
        Psl2ModeratePressureRating,
        Psl3HighPressureRating
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ShakerClass
    {
        Linear,
        Elliptical,
        DoubleDeck,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TankClass
    {
        Active,
        Reserve,
        Slug,
        Trip,
        Suction,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TankFluidType
    {
        DrillingMud,
        Brine,
        BaseOil,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AccumulatorClass
    {
        Bladder,
        Diaphragm,
        Piston,
        MetalBellow,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DerrickClass
    {
        Conventional,
        Slant,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HoistingSystemType
    {
        Drawworks,
        RamRig,
        RackAndPinion
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DrawworksClass
    {
        DirectDrive,
        SingleSpeedGearDriven,
        DualSpeedGearDriven,
        ActiveHeave,
        ActiveHeaveDualDrum,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RiserCompensatorClass
    {
        PassiveRiserTensioner,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CompensatorStatus
    {
        Active,
        Inactive
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum AutodrillerControlMode
    {
        Rop,
        Diffp,
        Trq,
        Limit,
        Wob
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MpdGradientMode
    {
        Single,
        Dual,
        Temperature,
        Flow
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MpdControlDeviceClass
    {
        Rotating,
        NonRotating
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ManifoldClass
    {
        Standalone,
        Integrated
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SurfaceMpdClass
    {
        BackPressurePump,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MarineMpdClass
    {
        RiserGasHandlingSystem,
        IntegratedRiserJoint,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SeparatorPhaseClass
    {
        TwoPhases,
        ThreePhases,
        FourPhases
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SolidsControlClass
    {
        Desander,
        Desilter,
        Degasser,
        Centrifuge,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum SeparatorMedium
    {
        Mud,
        WellboreFluid
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ControllerType
    {
        Hydraulic,
        Electrical,
        Acoustic,
        Pneumatic,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ChokeNumber
    {
        Single,
        Dual,
        Triple
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ChokeFunction
    {
        PressureControl,
        Safety
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum CasingDriveClass
    {
        Internal,
        External,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum MountingType
    {
        TruckMounted,
        SkidMounted,
        MiniUnit,
        Permanent,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BopStackClass
    {
        LandBop,
        MarineSurfaceBop,
        MarineSubSeaBop,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FloatValveClass
    {
        PlungerFloatValve,
        PortedPlungerValve,
        FlapperFloatValve,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum RiserClass
    {
        MarineDrillingRiser,
        TieBackDrillingRiser,
        BoltedRiser,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum FlowSensorType
    {
        Coriollis,
        Paddle,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum HeaveCompensatorClass
    {
        PassiveCrownMounted,
        ActiveCrownMounted,
        DirectLineCompensator,
        ActiveHeaveDrawworks,
        RigFloorCompensator,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ManifoldFlowPath
    {
        One,
        Two,
        Three
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DriveModeClass
    {
        Mechanical,
        ElectricalDcScr,
        ElectricalAcVfd,
        ElectroMechanical,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DrillingFluidClass
    {
        OilBased,
        WaterBased,
        GasBased
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum DrillingFluidType
    {
        AeratedMud,
        Air,
        BrackishWater,
        Brine,
        CaesiumFormate,
        DieselOilBased,
        EsterSyntheticBased,
        Freshwater,
        GlycolMud,
        GypMud,
        InternalOlefinSyntheticBased,
        LightlyTreatedNonDispersed,
        LigniteLignosulfonateMud,
        LimeMud,
        LinearParaffinSyntheticBased,
        LinearAlphaOlefinSyntheticBased,
        LowSolids,
        LowToxicityMineralOilBased,
        MineralOilBased,
        Mist,
        MixedMetalOxideMud,
        NativeNaturalMud,
        NaturalGas,
        NitrogenAeratedMud,
        NonAqueousInvertEmulsionDrillingFluids,
        NonDispersed,
        PneumaticGaseousDrillingFluids,
        PolymerMud,
        PotassiumFormate,
        PotassiumTreatedMud,
        SaltwaterMud,
        SaturatedSaltMud,
        SeaWater,
        SeawaterMud,
        SilicateMud,
        SodiumFormate,
        SpudMud,
        StableFoam,
        StiffFoam,
        WaterBasedDrillingFluids
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BopComponentClass
    {
        AnnularPreventer,
        BlindRam,
        BlindShearRam,
        Diverter,
        DrillingSpool,
        PipeRam,
        ShearRam,
        Other
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum BopLineClass
    {
        ChokeLine,
        KillLine,
        BoosterLine,
        SurfaceLine,
        Other
    }

    public class ShakerScreenDefinition
    {
        public int? ScreenDeck { get; set; }
        public string? MeshSize { get; set; }
    }

    public class CementPumpDisplacementPoint
    {
        public double? StrokeRate { get; set; }
        public double? FlowRate { get; set; }
        public double? Pressure { get; set; }
    }

    public class ChokeCvCurvePoint
    {
        public double? Pressure { get; set; }
        public double? Flow { get; set; }
    }

    public class RoutingManifoldCurvePoint
    {
        public double? Pressure { get; set; }
        public double? Flow { get; set; }
    }

    public class RheometerAfmMeasurement
    {
        public double? AfmViscShearRate { get; set; }
        public double? AfmViscShearStress { get; set; }
    }

    public class BopStackComponentDefinition
    {
        public BopComponentClass? BopStackComponentClass { get; set; }
        public double? BoreDiameter { get; set; }
        public double? Height { get; set; }
    }

    public class BopLineDefinition
    {
        public BopLineClass? BopLinesClass { get; set; }
        public double? LineOd { get; set; }
        public double? LineId { get; set; }
        public double? Length { get; set; }
    }
}
