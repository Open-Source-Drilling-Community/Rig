using System.Collections.Generic;

namespace NORCE.Drilling.Rig.Model
{
    public class RigMast : RigComponentBase
    {
        public HoistingSystem? HoistingSystem { get; set; }
        public CatWalk? CatWalk { get; set; }
        public PipeRack? PipeRack { get; set; }
        public CasingDriveSystem? CasingDriveSystem { get; set; }
        public CoilDriveSystem? CoilDriveSystem { get; set; }
        public Derrick? Derrick { get; set; }
        public TorqueTurnSub? TorqueTurnSub { get; set; }
        public RotaryTable? RotaryTable { get; set; }
        public TopDrive? TopDrive { get; set; }
        public Kelly? Kelly { get; set; }
        public IronRoughneck? IronRoughneck { get; set; }
        public CasingTongs? CasingTongs { get; set; }
        public CasingRunningTool? CasingRunningTool { get; set; }
        public StandPipe? StandPipe { get; set; }
        public StandPipeManifold? StandPipeManifold { get; set; }
        public RotaryHose? RotaryHose { get; set; }
        public ChokeManifold? ChokeManifold { get; set; }
        public List<RigChoke>? RigChokeList { get; set; }
        public Slips? Slips { get; set; }

        public RigMast() { }
    }
}
