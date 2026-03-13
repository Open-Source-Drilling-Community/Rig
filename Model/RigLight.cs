using OSDC.DotnetLibraries.General.DataManagement;
using System;

namespace NORCE.Drilling.Rig.Model
{
    /// <summary>
    /// Light weight version of a Rig
    /// Used to avoid loading the complete Rig (heavy weight data) each time we only need contextual info on the data
    /// Typically used for listing, sorting and filtering purposes
    /// </summary>
    public class RigLight
    {
        /// <summary>
        /// a MetaInfo for the RigLight
        /// </summary>
        public MetaInfo? MetaInfo { get; set; }

        /// <summary>
        /// name of the data
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// a description of the data
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// the date when the data was created
        /// </summary>
        public DateTimeOffset? CreationDate { get; set; }

        /// <summary>
        /// the date when the data was last modified
        /// </summary>
        public DateTimeOffset? LastModificationDate { get; set; }
        /// <summary>
        /// true if it is a fixed platform
        /// </summary>
        public bool IsFixedPlatform { get; set; }

        /// <summary>
        /// the ID of the cluster in the case of a fixed platform
        /// </summary>
        public Guid? ClusterID { get; set; }

        /// <summary>
        /// default constructor required for parsing the data model as a json file
        /// </summary>
        public RigLight() : base()
        {
        }

        /// <summary>
        /// base constructor
        /// </summary>
        public RigLight(MetaInfo? metaInfo, string? name, string? descr, DateTimeOffset? creationDate, DateTimeOffset? modifDate, bool isFixedPlatform, Guid? clusterID)
        {
            MetaInfo = metaInfo;
            Name = name;
            Description = descr;
            CreationDate = creationDate;
            LastModificationDate = modifDate;
            IsFixedPlatform = isFixedPlatform;
            ClusterID = clusterID;
        }
    }
}
