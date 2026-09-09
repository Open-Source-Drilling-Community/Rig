using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Rig.Model
{
    public class Rig
    {
        public MetaInfo? MetaInfo { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public DateTimeOffset? LastModificationDate { get; set; }
        public RigIdentification? Identification { get; set; }
        public RigType? RigType { get; set; }
        public RigEnvironment? OperatingEnvironment { get; set; }
        public RigMobilityType? MobilityType { get; set; }
        public RigOperatingEnvelope? OperatingEnvelope { get; set; }
        public MarineUnitProfile? MarineUnitProfile { get; set; }
        public JackUpProfile? JackUpProfile { get; set; }
        public StationKeepingSystem? StationKeepingSystem { get; set; }
        public List<RigStorageCapacity>? StorageCapacities { get; set; }
        public List<RigFeatureAssignment>? FeatureAssignments { get; set; }
        public List<MudPump>? MudPumpList { get; set; }
        public List<CementPump>? CementPumpList { get; set; }
        public CementUnit? CementUnit { get; set; }
        public DriveMode? DriveMode { get; set; }
        public RigMast? MainRigMast { get; set; }
        public RigMast? AuxiliaryRigMast { get; set; }
        public List<MudTank>? MudTankList { get; set; }
        public List<Generator>? GeneratorList { get; set; }
        public List<ShaleShaker>? ShaleShakerList { get; set; }
        public AuxSolidsControl? AuxSolidsControl { get; set; }
        public DrillingFluidTypeDescriptor? DrillingFluidType { get; set; }
        public FlowSensor? FlowSensor { get; set; }
        public MeasurementAfm? MeasurementAfm { get; set; }
        public ReturnFlowLine? ReturnFlowLine { get; set; }
        public List<MudGasSeparator>? MudGasSeparatorList { get; set; }
        public List<Desander>? DesanderList { get; set; }
        public List<Desilter>? DesilterList { get; set; }
        public List<Centrifuge>? CentrifugeList { get; set; }
        public List<Degasser>? DegasserList { get; set; }
        public CuttingsTransportSystem? CuttingsTransportSystem { get; set; }
        public List<CuttingsDryer>? CuttingsDryerList { get; set; }
        public PipeDeck? PipeDeck { get; set; }
        public Accumulator? Accumulator { get; set; }
        public BopStack? BopStack { get; set; }
        public FloatValve? FloatValve { get; set; }
        public AutoDriller? AutoDriller { get; set; }
        public MpdController? MpdController { get; set; }
        public MpdControlDevice? MpdControlDevice { get; set; }
        public ContinuousCirculationDevice? ContinuousCirculationDevice { get; set; }
        public DrillingChokeManifold? DrillingChokeManifold { get; set; }
        public SurfaceMpdEquipment? SurfaceMpdEquipment { get; set; }
        public MarineMpdEquipment? MarineMpdEquipment { get; set; }
        public MultiPhaseSeparator? MultiPhaseSeparator { get; set; }
        public FlowRoutingManifold? FlowRoutingManifold { get; set; }
        public DrillstringHeaveCompensator? DrillstringHeaveCompensator { get; set; }
        public DrillingMarineRiser? DrillingMarineRiser { get; set; }
        public RiserHeaveCompensator? RiserHeaveCompensator { get; set; }
        /// <summary>Properties available only for a RigType.PlatformRig.</summary>
        public FixedPlatformProperties? FixedPlatformProperties { get; set; }
        /// <summary>
        /// Deprecated compatibility property. Despite its historical name, the
        /// value is a depth in SI metres relative to WGS84 and is never sign-inverted
        /// when migrated to FixedPlatformProperties.DrillFloorDepth.
        /// </summary>
        [Obsolete("Use FixedPlatformProperties.DrillFloorDepth. This property will be removed after all consumers and stored data are migrated.")]
        public double? DrillFloorElevation { get; set; }
        /// <summary>Deprecated compatibility flag. RigType is authoritative.</summary>
        [Obsolete("Use RigType. This property will be removed after all consumers and stored data are migrated.")]
        public bool IsFixedPlatform { get; set; }
        public Guid? ClusterID { get; set; }

        public Rig() : base()
        {
        }
    }
}

