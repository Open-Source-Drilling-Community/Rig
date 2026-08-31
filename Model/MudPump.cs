using OSDC.DotnetLibraries.Drilling.DrillingProperties;
using OSDC.DotnetLibraries.General.DataManagement;
using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Rig.Model
{
    public class MudPump : RigEquipmentBase
    {
        public MudPumpType Type { get; set; }
        public PumpClass? PumpClass { get; set; }
        public int? PumpAction { get; set; }
        public double? PumpEfficiency { get; set; }
        public double? Stroke { get; set; }
        public List<MudPumpLinerConfiguration>? LinerConfigurations { get; set; }
        public double? PulsationDamperPressure { get; set; }
        public double? PulsationDamperVolume { get; set; }
        public double? MaxLimitDesignPressure { get; set; }
        public double? MaxLimitOperatingPower { get; set; }
        public double? MaxLimitOperatingSpeed { get; set; }
        public MudPump() : base()
        {
        }
    }
}
