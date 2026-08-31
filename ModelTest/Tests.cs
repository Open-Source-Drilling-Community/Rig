using OSDC.Drilling.Rig.Model;
using OSDC.DotnetLibraries.General.DataManagement;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OSDC.Drilling.Rig.ModelTest
{
    public class Tests
    {
        private static readonly Assembly ModelAssembly = typeof(Model.Rig).Assembly;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            Converters = { new JsonStringEnumConverter() }
        };

        private static IEnumerable<Type> ConcreteModelClasses() =>
            ModelAssembly
                .GetTypes()
                .Where(t => t.Namespace == typeof(Model.Rig).Namespace && t.IsClass && t.IsPublic && !t.IsAbstract && !t.IsNested)
                .OrderBy(t => t.Name);

        private static IEnumerable<Type> TopLevelModelEnums() =>
            ModelAssembly
                .GetTypes()
                .Where(t => t.Namespace == typeof(Model.Rig).Namespace && t.IsEnum && t.IsPublic && !t.IsNested)
                .OrderBy(t => t.Name);

        private static IEnumerable<Type> EquipmentTypes() =>
            ConcreteModelClasses()
                .Where(t => t.IsSubclassOf(typeof(RigEquipmentBase)));

        private static IEnumerable<Type> ComponentTypes() =>
            ConcreteModelClasses()
                .Where(t => t != typeof(Model.Rig)
                    && t != typeof(RigLight)
                    && !t.IsSubclassOf(typeof(RigEquipmentBase))
                    && t.IsSubclassOf(typeof(RigComponentBase)));

        private static IEnumerable<Type> HelperPointTypes()
        {
            yield return typeof(ShakerScreenDefinition);
            yield return typeof(CementPumpDisplacementPoint);
            yield return typeof(ChokeCvCurvePoint);
            yield return typeof(RoutingManifoldCurvePoint);
            yield return typeof(BopStackComponentDefinition);
            yield return typeof(BopLineDefinition);
            yield return typeof(CountPerDay);
        }

        [Test]
        public void MudPumpType_UsesModelNamespace()
        {
            Assert.That(typeof(MudPumpType).Namespace, Is.EqualTo(typeof(Model.Rig).Namespace));
        }

        [Test]
        public void AllConcreteModelClasses_HavePublicParameterlessConstructors()
        {
            List<string> failures = ConcreteModelClasses()
                .Where(t => t.GetConstructor(Type.EmptyTypes) == null)
                .Select(t => t.Name)
                .ToList();

            Assert.That(failures, Is.Empty, string.Join(", ", failures));
        }

        [Test]
        public void AllConcreteModelClasses_CanBeInstantiated()
        {
            List<string> failures = new();

            foreach (Type type in ConcreteModelClasses())
            {
                try
                {
                    object? instance = Activator.CreateInstance(type);
                    if (instance == null)
                    {
                        failures.Add(type.Name);
                    }
                }
                catch
                {
                    failures.Add(type.Name);
                }
            }

            Assert.That(failures, Is.Empty, string.Join(", ", failures));
        }

        [Test]
        public void AllConcreteModelClasses_CanRoundTripThroughJson()
        {
            List<string> failures = new();

            foreach (Type type in ConcreteModelClasses())
            {
                try
                {
                    object? instance = Activator.CreateInstance(type);
                    string json = JsonSerializer.Serialize(instance, type, JsonOptions);
                    object? roundTrip = JsonSerializer.Deserialize(json, type, JsonOptions);
                    if (roundTrip == null)
                    {
                        failures.Add(type.Name);
                    }
                }
                catch
                {
                    failures.Add(type.Name);
                }
            }

            Assert.That(failures, Is.Empty, string.Join(", ", failures));
        }

        [Test]
        public void AllPublicInstanceProperties_AreReadable()
        {
            List<string> failures = ConcreteModelClasses()
                .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(property => property.GetMethod == null)
                    .Select(property => $"{type.Name}.{property.Name}"))
                .ToList();

            Assert.That(failures, Is.Empty, string.Join(", ", failures));
        }

        [Test]
        public void AllPublicInstanceProperties_AreWritable_WhenDesignedAsDataModelProperties()
        {
            List<string> failures = ConcreteModelClasses()
                .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(property => property.SetMethod == null)
                    .Select(property => $"{type.Name}.{property.Name}"))
                .ToList();

            Assert.That(failures, Is.Empty, string.Join(", ", failures));
        }

        [Test]
        public void TopLevelModelEnums_UseStringJsonSerialization()
        {
            List<string> failures = TopLevelModelEnums()
                .Where(type => type.GetCustomAttribute<JsonConverterAttribute>()?.ConverterType != typeof(JsonStringEnumConverter))
                .Select(type => type.Name)
                .ToList();

            Assert.That(failures, Is.Empty, string.Join(", ", failures));
        }

        [Test]
        public void TopLevelModelEnums_SerializeToNamedValues()
        {
            List<string> failures = new();

            foreach (Type enumType in TopLevelModelEnums())
            {
                Array values = Enum.GetValues(enumType);
                if (values.Length == 0)
                {
                    failures.Add(enumType.Name);
                    continue;
                }

                object value = values.GetValue(0)!;
                string json = JsonSerializer.Serialize(value, enumType, JsonOptions);
                string expectedToken = $"\"{Enum.GetName(enumType, value)}\"";

                if (json != expectedToken)
                {
                    failures.Add(enumType.Name);
                }
            }

            Assert.That(failures, Is.Empty, string.Join(", ", failures));
        }

        [Test]
        public void Rig_UsesCorrectAuxiliaryRigMastPropertyName()
        {
            PropertyInfo? property = typeof(Model.Rig).GetProperty("AuxiliaryRigMast");

            Assert.That(property, Is.Not.Null);
            Assert.That(typeof(Model.Rig).GetProperty("AuxilliaryRigMast"), Is.Null);
            Assert.That(property!.PropertyType, Is.EqualTo(typeof(RigMast)));
        }

        [Test]
        public void Rig_SerializesCorrectAuxiliaryRigMastPropertyName()
        {
            Model.Rig rig = new()
            {
                AuxiliaryRigMast = new RigMast { Name = "Aux mast" }
            };

            string json = JsonSerializer.Serialize(rig, JsonOptions);

            Assert.That(json, Does.Contain("\"AuxiliaryRigMast\""));
            Assert.That(json, Does.Not.Contain("\"AuxilliaryRigMast\""));
        }

        [Test]
        public void Rig_UsesSpreadsheetEquipmentNamesForConsolidatedConcepts()
        {
            Assert.That(typeof(Model.Rig).GetProperty("MudTankList"), Is.Not.Null);
            Assert.That(typeof(Model.Rig).GetProperty("PitList"), Is.Null);
            Assert.That(typeof(RigMast).GetProperty("Derrick"), Is.Not.Null);
            Assert.That(typeof(RigMast).GetProperty("HoistingSystem"), Is.Not.Null);
        }

        [Test]
        public void MudPump_UsesLinerSpecificPerformanceConfigurations()
        {
            Assert.That(typeof(MudPump).GetProperty(nameof(MudPump.LinerConfigurations)), Is.Not.Null);
            Assert.That(typeof(MudPump).GetProperty("LinerId"), Is.Null);
            Assert.That(typeof(MudPump).GetProperty("PumpDisplacement"), Is.Null);
            Assert.That(typeof(MudPump).GetProperty("MaxLimitOperatingPressure"), Is.Null);
            Assert.That(typeof(MudPump).GetProperty("MaxLimitOperatingFlowRate"), Is.Null);

            MudPump pump = new()
            {
                Stroke = 0.3048,
                LinerConfigurations =
                [
                    new MudPumpLinerConfiguration
                    {
                        LinerInnerDiameter = 0.1778,
                        DisplacementPerStroke = 0.025,
                        MaximumVolumetricFlowRate = 0.05,
                        MaximumDischargePressure = 20_000_000
                    }
                ]
            };

            string json = JsonSerializer.Serialize(pump, JsonOptions);
            Assert.That(json, Does.Contain("\"LinerConfigurations\""));
            Assert.That(json, Does.Contain("\"MaximumDischargePressure\""));
        }

        [Test]
        public void Rig_ExposesExpectedSpreadsheetDrivenProperties()
        {
            string[] expectedProperties =
            {
                "MudPumpList",
                "CementPumpList",
                "CementUnit",
                "DriveMode",
                "MainRigMast",
                "AuxiliaryRigMast",
                "MudTankList",
                "GeneratorList",
                "ShaleShakerList",
                "AuxSolidsControl",
                "DrillingFluidType",
                "FlowSensor",
                "MeasurementAfm",
                "Accumulator",
                "BopStack",
                "AutoDriller",
                "MpdController",
                "SurfaceMpdEquipment",
                "MarineMpdEquipment",
                "MultiPhaseSeparator",
                "FlowRoutingManifold",
                "DrillstringHeaveCompensator",
                "DrillingMarineRiser",
                "RiserHeaveCompensator"
            };

            foreach (string propertyName in expectedProperties)
            {
                Assert.That(typeof(Model.Rig).GetProperty(propertyName), Is.Not.Null, propertyName);
            }
        }

        [Test]
        public void RigMast_ExposesDetailedEquipmentStructure()
        {
            string[] expectedProperties =
            {
                "CatWalk",
                "PipeRack",
                "CasingDriveSystem",
                "CoilDriveSystem",
                "Derrick",
                "HoistingSystem",
                "TorqueTurnSub",
                "RotaryTable",
                "TopDrive",
                "Kelly",
                "IronRoughneck",
                "CasingTongs",
                "CasingRunningTool",
                "StandPipe",
                "StandPipeManifold",
                "RotaryHose",
                "ChokeManifold",
                "RigChokeList",
                "Slips"
            };

            foreach (string propertyName in expectedProperties)
            {
                Assert.That(typeof(RigMast).GetProperty(propertyName), Is.Not.Null, propertyName);
            }
        }

        [Test]
        public void SpreadsheetEquipmentTypes_InheritSharedMetadataBase()
        {
            Assert.That(EquipmentTypes(), Is.Not.Empty);

            foreach (Type equipmentType in EquipmentTypes())
            {
                Assert.That(equipmentType.IsSubclassOf(typeof(RigEquipmentBase)), Is.True, equipmentType.Name);
                Assert.That(equipmentType.GetProperty(nameof(RigEquipmentBase.Name)), Is.Not.Null, equipmentType.Name);
                Assert.That(equipmentType.GetProperty(nameof(RigEquipmentBase.Manufacturer)), Is.Not.Null, equipmentType.Name);
                Assert.That(equipmentType.GetProperty(nameof(RigEquipmentBase.MeasurementCapabilities)), Is.Not.Null, equipmentType.Name);
            }
        }

        [Test]
        public void RigMasterData_DoesNotExposeSpreadsheetLiveMeasurements()
        {
            (Type Type, string Property)[] removedMeasurements =
            {
                (typeof(Generator), "EnginePower"),
                (typeof(TopDrive), "TopDriveHeight"),
                (typeof(Kelly), "SurfaceRotation"),
                (typeof(TorqueTurnSub), "SurfaceTorque"),
                (typeof(CoilDriveSystem), "CtLoad"),
                (typeof(CrownBlock), "Hookload"),
                (typeof(TravellingBlock), "HookVelocity"),
                (typeof(DrillLine), "Hookload"),
                (typeof(BopStack), "CasingPressure"),
                (typeof(MudPump), "MudPumpStrokeRate"),
                (typeof(AuxSolidsControl), "DesanderOn"),
                (typeof(FlowSensor), "MudFlowrateOut"),
                (typeof(MeasurementAfm), "AfmMudDensity"),
                (typeof(AutoDriller), "SetpointWob"),
                (typeof(MpdController), "ControlledDownholePressure"),
                (typeof(SurfaceMpdEquipment), "PressureAtDischarge"),
                (typeof(DrillingChokeManifold), "PressureBeforeChoke"),
                (typeof(FlowRoutingManifold), "InletPressure"),
                (typeof(MultiPhaseSeparator), "PressureSeparator")
            };

            foreach ((Type type, string property) in removedMeasurements)
                Assert.That(type.GetProperty(property), Is.Null, $"{type.Name}.{property}");
        }

        [Test]
        public void WeakSpreadsheetFields_AreRepresentedStructurally()
        {
            Assert.That(typeof(BopStack).GetProperty(nameof(BopStack.UnitReferences))!.PropertyType, Is.EqualTo(typeof(List<string>)));
            Assert.That(typeof(CementUnit).GetProperty(nameof(CementUnit.Capabilities))!.PropertyType, Is.EqualTo(typeof(List<string>)));
            Assert.That(typeof(DrillingChokeManifold).GetProperty(nameof(DrillingChokeManifold.ChokeCount))!.PropertyType, Is.EqualTo(typeof(int?)));
            Assert.That(typeof(DrillingChokeManifold).GetProperty(nameof(DrillingChokeManifold.FlowMeterCount))!.PropertyType, Is.EqualTo(typeof(int?)));
            Assert.That(typeof(DrillingChokeManifold).GetProperty(nameof(DrillingChokeManifold.PressureSensorVotingNumber))!.PropertyType, Is.EqualTo(typeof(int?)));
        }

        [Test]
        public void LightweightComponentTypes_InheritSharedComponentBase()
        {
            Assert.That(ComponentTypes(), Is.Not.Empty);

            foreach (Type componentType in ComponentTypes())
            {
                Assert.That(componentType.IsSubclassOf(typeof(RigComponentBase)), Is.True, componentType.Name);
                Assert.That(componentType.GetProperty(nameof(RigComponentBase.Name)), Is.Not.Null, componentType.Name);
                Assert.That(componentType.GetProperty(nameof(RigComponentBase.Description)), Is.Not.Null, componentType.Name);
            }
        }

        [Test]
        public void TopDrive_RetainsControllerConfigurationProperties()
        {
            string[] expectedProperties =
            {
                "ProportionalGain",
                "IntegralGain",
                "TuningFrequency",
                "VFDFilterTimeConstant",
                "EncoderTimeConstant",
                "AccelerationFilterTimeConstant",
                "TorqueHighPassFilterTimeConstant",
                "TorqueLowPassFilterTimeConstant",
                "TuningFactor",
                "InertiaCorrectionFactor"
            };

            foreach (string propertyName in expectedProperties)
            {
                Assert.That(typeof(TopDrive).GetProperty(propertyName), Is.Not.Null, propertyName);
            }

            string[] expectedControllerValues =
            {
                "Unknown",
                "StiffPIController",
                "TunedPIController",
                "ImpedanceMatching"
            };

            Assert.That(Enum.GetNames(typeof(TopDriveControllerType)), Is.EqualTo(expectedControllerValues));
        }

        [Test]
        public void Rig_ExposesCompatibleMasterDataLift()
        {
            string[] properties =
            {
                nameof(Model.Rig.Identification), nameof(Model.Rig.RigType), nameof(Model.Rig.OperatingEnvironment),
                nameof(Model.Rig.MobilityType), nameof(Model.Rig.OperatingEnvelope), nameof(Model.Rig.MarineUnitProfile),
                nameof(Model.Rig.JackUpProfile), nameof(Model.Rig.StationKeepingSystem), nameof(Model.Rig.StorageCapacities),
                nameof(Model.Rig.FeatureAssignments)
            };
            foreach (string property in properties) Assert.That(typeof(Model.Rig).GetProperty(property), Is.Not.Null, property);
            Assert.That(typeof(Model.Rig).GetProperty(nameof(Model.Rig.IsFixedPlatform)), Is.Not.Null);
        }

        [Test]
        public void TopDrive_AddsCommercialRatingsWithoutRemovingControllerConfiguration()
        {
            string[] additions =
            {
                nameof(TopDrive.RatedPower), nameof(TopDrive.RatedHoistingCapacity), nameof(TopDrive.RatedContinuousTorque),
                nameof(TopDrive.RatedIntermittentTorque), nameof(TopDrive.MotorCount), nameof(TopDrive.MotorType),
                nameof(TopDrive.IbopConfiguration), nameof(TopDrive.AutomationSystemCompatibility)
            };
            foreach (string property in additions) Assert.That(typeof(TopDrive).GetProperty(property), Is.Not.Null, property);
        }

        [Test]
        public void HelperPointTypes_CanRoundTripThroughJson()
        {
            List<string> failures = new();

            foreach (Type type in HelperPointTypes())
            {
                try
                {
                    object instance = Activator.CreateInstance(type)!;
                    string json = JsonSerializer.Serialize(instance, type, JsonOptions);
                    object? roundTrip = JsonSerializer.Deserialize(json, type, JsonOptions);
                    if (roundTrip == null)
                    {
                        failures.Add(type.Name);
                    }
                }
                catch
                {
                    failures.Add(type.Name);
                }
            }

            Assert.That(failures, Is.Empty, string.Join(", ", failures));
        }

        [Test]
        public void History_IncrementInitializesTodayAndAccumulatesCount()
        {
            History history = new();

            history.Increment();
            history.Increment();

            Assert.That(history.Data, Has.Count.EqualTo(1));
            Assert.That(history.Data[0].Date, Is.EqualTo(DateTime.UtcNow.Date));
            Assert.That(history.Data[0].Count, Is.EqualTo(2UL));
        }

        [Test]
        public void CountPerDay_InitializationConstructor_AssignsValues()
        {
            DateTime date = new(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
            CountPerDay countPerDay = new(date, 5);

            Assert.That(countPerDay.Date, Is.EqualTo(date));
            Assert.That(countPerDay.Count, Is.EqualTo(5UL));
        }

        [Test]
        public void UsageStatisticsRig_IncrementMethods_CreateAndUpdateHistory()
        {
            UsageStatisticsRig usageStatistics = new()
            {
                LastSaved = DateTime.UtcNow,
                BackUpInterval = TimeSpan.FromDays(365)
            };

            usageStatistics.GetAllRigIdPerDay = null!;
            usageStatistics.GetAllRigMetaInfoPerDay = null!;
            usageStatistics.GetRigByIdPerDay = null!;
            usageStatistics.GetAllRigLightPerDay = null!;
            usageStatistics.GetAllRigPerDay = null!;
            usageStatistics.PostRigPerDay = null!;
            usageStatistics.PutRigByIdPerDay = null!;
            usageStatistics.DeleteRigByIdPerDay = null!;
            usageStatistics.BatchExportRigsPerDay = null!;
            usageStatistics.BatchRestoreRigsPerDay = null!;

            usageStatistics.IncrementGetAllRigIdPerDay();
            usageStatistics.IncrementGetAllRigMetaInfoPerDay();
            usageStatistics.IncrementGetRigByIdPerDay();
            usageStatistics.IncrementGetAllRigLightPerDay();
            usageStatistics.IncrementGetAllRigPerDay();
            usageStatistics.IncrementPostRigPerDay();
            usageStatistics.IncrementPutRigByIdPerDay();
            usageStatistics.IncrementDeleteRigByIdPerDay();
            usageStatistics.IncrementBatchExportRigsPerDay();
            usageStatistics.IncrementBatchRestoreRigsPerDay();

            History[] histories =
            {
                usageStatistics.GetAllRigIdPerDay,
                usageStatistics.GetAllRigMetaInfoPerDay,
                usageStatistics.GetRigByIdPerDay,
                usageStatistics.GetAllRigLightPerDay,
                usageStatistics.GetAllRigPerDay,
                usageStatistics.PostRigPerDay,
                usageStatistics.PutRigByIdPerDay,
                usageStatistics.DeleteRigByIdPerDay,
                usageStatistics.BatchExportRigsPerDay,
                usageStatistics.BatchRestoreRigsPerDay
            };

            foreach (History history in histories)
            {
                Assert.That(history, Is.Not.Null);
                Assert.That(history.Data, Has.Count.EqualTo(1));
                Assert.That(history.Data[0].Count, Is.EqualTo(1UL));
            }
        }

        [Test]
        public void RigLight_InitializationConstructor_AssignsValues()
        {
            MetaInfo metaInfo = new();
            Guid clusterId = Guid.NewGuid();
            DateTimeOffset creationDate = new(2026, 3, 10, 12, 0, 0, TimeSpan.Zero);
            DateTimeOffset modificationDate = creationDate.AddHours(2);

            RigLight rigLight = new(metaInfo, "Rig A", "Test rig", creationDate, modificationDate, true, clusterId);

            Assert.That(rigLight.MetaInfo, Is.SameAs(metaInfo));
            Assert.That(rigLight.Name, Is.EqualTo("Rig A"));
            Assert.That(rigLight.Description, Is.EqualTo("Test rig"));
            Assert.That(rigLight.CreationDate, Is.EqualTo(creationDate));
            Assert.That(rigLight.LastModificationDate, Is.EqualTo(modificationDate));
            Assert.That(rigLight.IsFixedPlatform, Is.True);
            Assert.That(rigLight.ClusterID, Is.EqualTo(clusterId));
        }
    }
}
