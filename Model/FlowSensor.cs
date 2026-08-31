namespace OSDC.Drilling.Rig.Model
{
    public class FlowSensor : RigEquipmentBase
    {
        public FlowSensorType? FlowTransducer { get; set; }
        public bool? FlowOutOfBorehole { get; set; }
        public FlowSensor() { }
    }
}



