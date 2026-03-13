using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace NORCE.Drilling.Rig.Model
{
    public class Rig
    {
        public MetaInfo? MetaInfo { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public DateTimeOffset? CreationDate { get; set; }
        public DateTimeOffset? LastModificationDate { get; set; }
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
        public double? DrillFloorElevation { get; set; }
        public bool IsFixedPlatform { get; set; }
        public Guid? ClusterID { get; set; }

        public Rig() : base()
        {
        }
    }
}

