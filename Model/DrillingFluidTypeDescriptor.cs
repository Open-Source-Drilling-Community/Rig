namespace NORCE.Drilling.Rig.Model
{
    public class DrillingFluidTypeDescriptor : RigComponentBase
    {
        public DrillingFluidClass? DrillingFluidClass { get; set; }
        public DrillingFluidType? DrillingFluidType { get; set; }

        public DrillingFluidTypeDescriptor() { }
    }
}
