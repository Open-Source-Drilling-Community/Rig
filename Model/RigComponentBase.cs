using System;
using System.Collections.Generic;

namespace OSDC.Drilling.Rig.Model
{
    public abstract class RigComponentBase
    {
        /// <summary>Stable identity of this physical or logical component within the rig definition.</summary>
        public Guid? ID { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public abstract class RigEquipmentBase : RigComponentBase
    {
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? ProductCode { get; set; }
        public string? SerialNumber { get; set; }
        public string? AssetTag { get; set; }
        public DateTimeOffset? InstallationDate { get; set; }
        public DateTimeOffset? CommissioningDate { get; set; }
        public EquipmentLifecycleStatus? LifecycleStatus { get; set; }
        public List<string>? CertificationReferences { get; set; }
        /// <summary>
        /// Measurements that this equipment can provide. These records describe
        /// installed instrumentation and calculated measurement capabilities; they
        /// do not contain live measurement values.
        /// </summary>
        public List<EquipmentMeasurementCapability>? MeasurementCapabilities { get; set; }
    }
}
