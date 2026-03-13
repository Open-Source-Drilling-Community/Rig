namespace NORCE.Drilling.Rig.Model
{
    public class AuxSolidsControl : RigEquipmentBase
    {
        public SolidsControlClass? SolidsControlClass { get; set; }
        public bool? DesanderOn { get; set; }
        public bool? DesilterOn { get; set; }
        public bool? DegasserOn { get; set; }
        public bool? CentrifugeOn { get; set; }

        public AuxSolidsControl() { }
    }
}



