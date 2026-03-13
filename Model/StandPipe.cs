namespace NORCE.Drilling.Rig.Model
{
    public class StandPipe : RigComponentBase
    {
        public double? PressureMeasurementElevation { get; set; }
        public double? MudHoseHangingPointElevation { get; set; }

        public StandPipe() { }
    }
}
