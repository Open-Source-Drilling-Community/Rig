namespace NORCE.Drilling.Rig.Model
{
    public class FlowSensor : RigEquipmentBase
    {
        public FlowSensorType? FlowTransducer { get; set; }
        public bool? FlowOutOfBorehole { get; set; }
        public double? MudFlowrateOut { get; set; }
        public double? MudFlowrateIn { get; set; }

        public FlowSensor() { }
    }
}



