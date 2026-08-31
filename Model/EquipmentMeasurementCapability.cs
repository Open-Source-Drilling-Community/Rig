using System;

namespace OSDC.Drilling.Rig.Model
{
    /// <summary>
    /// Describes an installed or calculated measurement capability without storing
    /// a current measurement value. Numeric range and absolute-accuracy values use
    /// the SI unit identified by <see cref="PhysicalQuantity"/>.
    /// </summary>
    public class EquipmentMeasurementCapability : RigComponentBase
    {
        /// <summary>Stable machine-readable measurement name, for example standpipe_pressure.</summary>
        public string? MeasurementCode { get; set; }

        /// <summary>OSDC physical-quantity name governing SI storage and unit-aware display.</summary>
        public string? PhysicalQuantity { get; set; }

        /// <summary>How the measurement is obtained.</summary>
        public MeasurementSourceKind? SourceKind { get; set; }

        /// <summary>Human-readable transducer or calculation type.</summary>
        public string? SourceType { get; set; }

        /// <summary>Optional component whose signal is used when it differs from the containing equipment.</summary>
        public Guid? SourceComponentID { get; set; }

        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? ProductCode { get; set; }
        public string? SerialNumber { get; set; }

        /// <summary>Lower measurable value in the SI unit of PhysicalQuantity.</summary>
        public double? MinimumValue { get; set; }

        /// <summary>Upper measurable value in the SI unit of PhysicalQuantity.</summary>
        public double? MaximumValue { get; set; }

        /// <summary>Absolute accuracy in the SI unit of PhysicalQuantity.</summary>
        public double? AbsoluteAccuracy { get; set; }

        /// <summary>Relative accuracy as a dimensionless fraction, where 0.01 means one percent.</summary>
        public double? RelativeAccuracy { get; set; }

        /// <summary>Nominal update frequency in hertz.</summary>
        public double? UpdateFrequency { get; set; }
    }
}
