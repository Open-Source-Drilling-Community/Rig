using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Rig.Model;
using OSDC.Drilling.Rig.Service;
using OSDC.Drilling.Rig.Service.Controllers;
using OSDC.Drilling.Rig.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;
using System.Reflection;
using System.Text.Json;

namespace ServiceTest
{
    public class Tests
    {
        private string _databaseFilePath = null!;

        private ILoggerFactory _loggerFactory = null!;
        private SqlConnectionManager _connectionManager = null!;
        private RigController _controller = null!;
        private RigFeatureCategoryController _featureController = null!;

        [SetUp]
        public void SetUp()
        {
            _loggerFactory = LoggerFactory.Create(builder => { });
            ResetRigManagerSingleton();
            string testDirectory = Path.Combine(Path.GetTempPath(), "OSDC.Drilling.Rig.ServiceTest");
            Directory.CreateDirectory(testDirectory);
            _databaseFilePath = Path.Combine(testDirectory, $"Rig-{Guid.NewGuid():N}.db");

            _connectionManager = new SqlConnectionManager(
                $"Data Source={_databaseFilePath}",
                _loggerFactory.CreateLogger<SqlConnectionManager>());

            _controller = new RigController(_loggerFactory.CreateLogger<RigManager>(), _connectionManager);
            _featureController = new RigFeatureCategoryController(_loggerFactory.CreateLogger<RigFeatureCategoryManager>(), _connectionManager);
        }

        [TearDown]
        public void TearDown()
        {
            ResetRigManagerSingleton();
            _loggerFactory.Dispose();
            SqliteConnection.ClearAllPools();
            DeleteTestDatabaseFile(_databaseFilePath);
        }

        [Test]
        public void GetAllRigId_ReturnsEmptyList_WhenDatabaseIsEmpty()
        {
            ActionResult<IEnumerable<Guid>> actionResult = _controller.GetAllRigId();

            List<Guid> ids = AssertOk<List<Guid>>(actionResult.Result);

            Assert.That(ids, Is.Empty);
        }

        [Test]
        public void GetAllRigMetaInfo_ReturnsEmptyList_WhenDatabaseIsEmpty()
        {
            ActionResult<IEnumerable<MetaInfo>> actionResult = _controller.GetAllRigMetaInfo();

            List<MetaInfo?> metaInfos = AssertOk<List<MetaInfo?>>(actionResult.Result);

            Assert.That(metaInfos, Is.Empty);
        }

        [Test]
        public void GetAllRigLight_ReturnsEmptyList_WhenDatabaseIsEmpty()
        {
            ActionResult<IEnumerable<RigLight>> actionResult = _controller.GetAllRigLight();

            List<RigLight> rigLights = AssertOk<List<RigLight>>(actionResult.Result);

            Assert.That(rigLights, Is.Empty);
        }

        [Test]
        public void GetAllRig_ReturnsEmptyList_WhenDatabaseIsEmpty()
        {
            ActionResult<IEnumerable<RigReadResponse?>> actionResult = _controller.GetAllRig();

            List<RigReadResponse?> rigs = AssertOk<List<RigReadResponse?>>(actionResult.Result);

            Assert.That(rigs, Is.Empty);
        }

        [Test]
        public void GetRigById_ReturnsBadRequest_ForEmptyGuid()
        {
            ActionResult<RigReadResponse?> actionResult = _controller.GetRigById(Guid.Empty);

            Assert.That(actionResult.Result, Is.TypeOf<BadRequestResult>());
        }

        [Test]
        public void GetRigById_ReturnsNotFound_WhenRigDoesNotExist()
        {
            ActionResult<RigReadResponse?> actionResult = _controller.GetRigById(Guid.NewGuid());

            Assert.That(actionResult.Result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public void PostRig_ReturnsBadRequest_WhenPayloadIsNull()
        {
            ActionResult actionResult = _controller.PostRig(null);

            Assert.That(actionResult, Is.TypeOf<BadRequestResult>());
        }

        [Test]
        public void PostRig_ReturnsBadRequest_WhenRigIdIsEmpty()
        {
            Rig rig = CreateRig(Guid.Empty, "invalid-rig");

            ActionResult actionResult = _controller.PostRig(rig);

            Assert.That(actionResult, Is.TypeOf<BadRequestResult>());
        }

        [Test]
        public void PostRig_ReturnsConflict_WhenRigAlreadyExists()
        {
            Rig rig = CreateRig(Guid.NewGuid(), "duplicate-rig");

            Assert.That(_controller.PostRig(rig), Is.TypeOf<OkResult>());

            ActionResult duplicateResult = _controller.PostRig(rig);

            Assert.That(duplicateResult, Is.TypeOf<StatusCodeResult>());
            Assert.That(((StatusCodeResult)duplicateResult).StatusCode, Is.EqualTo(409));
        }

        [Test]
        public void PostRig_PersistsRig_And_AllReadEndpointsReturnConsistentData()
        {
            Guid id = Guid.NewGuid();
            Rig rig = CreateRig(id, "persisted-rig");

            Assert.That(_controller.PostRig(rig), Is.TypeOf<OkResult>());

            List<Guid> ids = AssertOk<List<Guid>>(_controller.GetAllRigId().Result);
            Assert.That(ids, Does.Contain(id));

            List<MetaInfo?> metaInfos = AssertOk<List<MetaInfo?>>(_controller.GetAllRigMetaInfo().Result);
            Assert.That(metaInfos.Any(x => x?.ID == id), Is.True);

            RigReadResponse persistedRig = AssertOk<RigReadResponse>(_controller.GetRigById(id).Result);
            Assert.That(persistedRig.MetaInfo?.ID, Is.EqualTo(id));
            Assert.That(persistedRig.Name, Is.EqualTo(rig.Name));
            Assert.That(persistedRig.Description, Is.EqualTo(rig.Description));

            List<RigLight> rigLights = AssertOk<List<RigLight>>(_controller.GetAllRigLight().Result);
            RigLight rigLight = rigLights.Single(x => x.MetaInfo?.ID == id);
            Assert.That(rigLight.Name, Is.EqualTo(rig.Name));
            Assert.That(rigLight.Description, Is.EqualTo(rig.Description));
            Assert.That(rigLight.CreationDate, Is.EqualTo(rig.CreationDate));
            Assert.That(rigLight.LastModificationDate, Is.EqualTo(rig.LastModificationDate));
            Assert.That(rigLight.IsFixedPlatform, Is.EqualTo(rig.IsFixedPlatform));
            Assert.That(rigLight.ClusterID, Is.EqualTo(rig.ClusterID));
            Assert.That(rigLight.RigType, Is.EqualTo(rig.RigType));
            Assert.That(rigLight.OperatingEnvironment, Is.EqualTo(rig.OperatingEnvironment));
            Assert.That(rigLight.MobilityType, Is.EqualTo(rig.MobilityType));

            List<RigReadResponse?> rigs = AssertOk<List<RigReadResponse?>>(_controller.GetAllRig().Result);
            Assert.That(rigs.Any(x => x?.MetaInfo?.ID == id && x.Name == rig.Name), Is.True);
        }

        [Test]
        public void PutRigById_ReturnsBadRequest_WhenPayloadIsNull()
        {
            ActionResult actionResult = _controller.PutRigById(Guid.NewGuid(), DateTimeOffset.UtcNow, null).Result!;

            Assert.That(actionResult, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public void PutRigById_ReturnsBadRequest_WhenRouteIdDoesNotMatchPayloadId()
        {
            Rig rig = CreateRig(Guid.NewGuid(), "mismatch-rig");

            ActionResult actionResult = _controller.PutRigById(Guid.NewGuid(), rig.LastModificationDate!.Value, rig).Result!;

            Assert.That(actionResult, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public void PutRigById_ReturnsNotFound_WhenRigDoesNotExist()
        {
            Guid id = Guid.NewGuid();
            Rig rig = CreateRig(id, "missing-rig");

            ActionResult actionResult = _controller.PutRigById(id, rig.LastModificationDate!.Value, rig).Result!;

            Assert.That(actionResult, Is.TypeOf<NotFoundObjectResult>());
        }

        [Test]
        public void PutRigById_UpdatesExistingRig()
        {
            Guid id = Guid.NewGuid();
            Rig rig = CreateRig(id, "original-rig");
            Assert.That(_controller.PostRig(rig), Is.TypeOf<OkResult>());

            DateTimeOffset? originalLastModification = rig.LastModificationDate;
            rig.Name = "updated-rig";
            rig.Description = "updated-description";
            rig.IsFixedPlatform = !rig.IsFixedPlatform;

            ActionResult<Rig> actionResult = _controller.PutRigById(id, originalLastModification!.Value, rig);

            Rig response = AssertOk<Rig>(actionResult.Result);
            Assert.That(response.LastModificationDate, Is.GreaterThanOrEqualTo(originalLastModification));

            RigReadResponse updatedRig = AssertOk<RigReadResponse>(_controller.GetRigById(id).Result);
            Assert.That(updatedRig.Name, Is.EqualTo("updated-rig"));
            Assert.That(updatedRig.Description, Is.EqualTo("updated-description"));
            Assert.That(updatedRig.IsFixedPlatform, Is.EqualTo(rig.IsFixedPlatform));
            Assert.That(updatedRig.LastModificationDate, Is.Not.Null);
            Assert.That(updatedRig.LastModificationDate, Is.GreaterThanOrEqualTo(originalLastModification));
        }

        [Test]
        public void PutRigById_RejectsStaleModificationTimestampWithoutChangingRig()
        {
            Guid id = Guid.NewGuid();
            Rig rig = CreateRig(id, "concurrent-rig");
            Assert.That(_controller.PostRig(rig), Is.TypeOf<OkResult>());
            DateTimeOffset expected = rig.LastModificationDate!.Value;

            rig.Name = "first update";
            Assert.That(_controller.PutRigById(id, expected, rig).Result, Is.TypeOf<OkObjectResult>());
            rig.Name = "stale update";
            ActionResult<Rig> stale = _controller.PutRigById(id, expected, rig);

            Assert.That(stale.Result, Is.TypeOf<ConflictObjectResult>());
            RigReadResponse stored = AssertOk<RigReadResponse>(_controller.GetRigById(id).Result);
            Assert.That(stored.Name, Is.EqualTo("first update"));
        }

        [Test]
        public void DeleteRigById_ReturnsNotFound_WhenRigDoesNotExist()
        {
            ActionResult actionResult = _controller.DeleteRigById(Guid.NewGuid());

            Assert.That(actionResult, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public void DeleteRigById_RemovesExistingRig()
        {
            Guid id = Guid.NewGuid();
            Rig rig = CreateRig(id, "deletable-rig");
            Assert.That(_controller.PostRig(rig), Is.TypeOf<OkResult>());

            ActionResult deleteResult = _controller.DeleteRigById(id);

            Assert.That(deleteResult, Is.TypeOf<OkResult>());
            Assert.That(_controller.GetRigById(id).Result, Is.TypeOf<NotFoundResult>());

            List<Guid> ids = AssertOk<List<Guid>>(_controller.GetAllRigId().Result);
            Assert.That(ids, Does.Not.Contain(id));
        }

        [Test]
        public void RigPhotos_AreSeparateFromDefaultRead_AndIncludedOnRequest()
        {
            Guid rigId = Guid.NewGuid();
            Assert.That(_controller.PostRig(CreateRig(rigId, "photo-rig")), Is.TypeOf<OkResult>());
            RigPhotoManager photos = new(_connectionManager);
            byte[] png = [137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 0];
            RigPhotoMetadata? created = photos.Create(rigId, "rig.png", "image/png", png,
                new RigPhotoMetadata { Title = "Rig floor", AlternativeText = "Rig floor", IsPrimary = true }, out string? error);

            Assert.That(error, Is.Null);
            Assert.That(created?.MetaInfo?.ID, Is.Not.EqualTo(Guid.Empty));
            RigReadResponse defaultRead = AssertOk<RigReadResponse>(_controller.GetRigById(rigId).Result);
            RigReadResponse mediaRead = AssertOk<RigReadResponse>(_controller.GetRigById(rigId, includePhotos: true).Result);
            Assert.That(defaultRead.Photos, Is.Null);
            Assert.That(JsonSerializer.Serialize(defaultRead, JsonSettings.Options), Does.Not.Contain("\"Photos\""));
            Assert.That(mediaRead.Photos, Has.Count.EqualTo(1));
            Assert.That(mediaRead.Photos![0].ByteLength, Is.EqualTo(png.Length));
            Assert.That(mediaRead.Photos[0].Sha256, Is.Not.Empty);

            Assert.That(_controller.DeleteRigById(rigId), Is.TypeOf<OkResult>());
            Assert.That(photos.GetAll(rigId), Is.Empty);
        }

        [Test]
        public void FeatureCatalog_SeedsStableBuiltIns_AndProtectsThem()
        {
            List<RigFeatureCategory> categories = AssertOk<List<RigFeatureCategory>>(_featureController.GetAll().Result);
            Assert.That(categories, Has.Count.EqualTo(7));
            Assert.That(categories, Has.All.Property(nameof(RigFeatureCategory.IsBuiltIn)).True);
            RigFeatureCategory walking = categories.Single(value => value.Code == "mobility-deployment");
            Assert.That(walking.Options, Has.Some.Property(nameof(RigFeatureOption.Code)).EqualTo("walking"));
            Assert.That(_featureController.Delete(walking.MetaInfo!.ID), Is.TypeOf<ConflictObjectResult>());
        }

        [Test]
        public void FeatureCatalog_CustomLifecycle_UsesServerIdsAndOptimisticConcurrency()
        {
            RigFeatureCategory request = new()
            {
                Name = "Operator classification",
                Options = [new RigFeatureOption { Name = "Preferred" }]
            };
            RigFeatureCategory created = AssertOk<RigFeatureCategory>(_featureController.Create(request).Result);
            Assert.That(created.MetaInfo?.ID, Is.Not.EqualTo(Guid.Empty));
            Assert.That(created.Options!.Single().ID, Is.Not.EqualTo(Guid.Empty));
            Assert.That(created.IsBuiltIn, Is.False);

            created.Description = "Updated";
            ActionResult<RigFeatureCategory> stale = _featureController.Update(created.MetaInfo!.ID, created.LastModificationDate!.Value.AddSeconds(-1), created);
            Assert.That(stale.Result, Is.TypeOf<ConflictObjectResult>());

            RigFeatureCategory updated = AssertOk<RigFeatureCategory>(_featureController.Update(created.MetaInfo.ID, created.LastModificationDate, created).Result);
            Assert.That(updated.Description, Is.EqualTo("Updated"));
            Assert.That(_featureController.Delete(updated.MetaInfo!.ID), Is.TypeOf<OkResult>());
        }

        [Test]
        public void RigCreate_ValidatesFeatureReferencesAndProtectsReferencedCategory()
        {
            RigFeatureCategory custom = AssertOk<RigFeatureCategory>(_featureController.Create(new RigFeatureCategory
            {
                Name = "Local capability",
                Options = [new RigFeatureOption { Name = "Available" }]
            }).Result);
            Rig rig = CreateRig(Guid.NewGuid(), "featured-rig");
            rig.FeatureAssignments = [new RigFeatureAssignment
            {
                ID = Guid.NewGuid(), FeatureCategoryID = custom.MetaInfo!.ID, FeatureOptionID = custom.Options!.Single().ID
            }];
            Assert.That(_controller.PostRig(rig), Is.TypeOf<OkResult>());
            Assert.That(_featureController.Delete(custom.MetaInfo.ID), Is.TypeOf<ConflictObjectResult>());

            Rig invalid = CreateRig(Guid.NewGuid(), "invalid-featured-rig");
            invalid.FeatureAssignments = [new RigFeatureAssignment
            {
                ID = Guid.NewGuid(), FeatureCategoryID = custom.MetaInfo.ID, FeatureOptionID = Guid.NewGuid()
            }];
            Assert.That(_controller.PostRig(invalid), Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public void RigCreate_ValidatesMudPumpLinerPerformanceRows()
        {
            Rig invalid = CreateRig(Guid.NewGuid(), "invalid-liner-table");
            invalid.MudPumpList =
            [
                new MudPump
                {
                    Name = "Mud pump 1",
                    MaxLimitDesignPressure = 30_000_000,
                    LinerConfigurations =
                    [
                        new MudPumpLinerConfiguration
                        {
                            LinerInnerDiameter = 0.1524,
                            MaximumVolumetricFlowRate = 0,
                            MaximumDischargePressure = 40_000_000
                        }
                    ]
                }
            ];

            BadRequestObjectResult result = (BadRequestObjectResult)_controller.PostRig(invalid);
            string json = JsonSerializer.Serialize(result.Value, JsonSettings.Options);
            Assert.That(json, Does.Contain("MaximumVolumetricFlowRate"));
            Assert.That(json, Does.Contain("must not exceed the pump MaxLimitDesignPressure"));
        }

        [Test]
        public void RigCreate_ValidatesEquipmentMeasurementCapabilities()
        {
            Rig invalid = CreateRig(Guid.NewGuid(), "invalid-instrumentation");
            invalid.MainRigMast!.TopDrive = new TopDrive
            {
                Name = "Top drive",
                MeasurementCapabilities =
                [
                    new EquipmentMeasurementCapability
                    {
                        MeasurementCode = "surface_torque",
                        PhysicalQuantity = "TorqueDrilling",
                        SourceKind = MeasurementSourceKind.Sensor,
                        MinimumValue = 100,
                        MaximumValue = 10,
                        RelativeAccuracy = 1.1,
                        UpdateFrequency = 0
                    },
                    new EquipmentMeasurementCapability
                    {
                        MeasurementCode = "SURFACE_TORQUE",
                        PhysicalQuantity = "TorqueDrilling",
                        SourceKind = MeasurementSourceKind.Calculated,
                        SourceComponentID = Guid.NewGuid()
                    }
                ]
            };

            BadRequestObjectResult result = (BadRequestObjectResult)_controller.PostRig(invalid);
            string json = JsonSerializer.Serialize(result.Value, JsonSettings.Options);
            Assert.That(json, Does.Contain("SourceType is required"));
            Assert.That(json, Does.Contain("MinimumValue must not exceed MaximumValue"));
            Assert.That(json, Does.Contain("RelativeAccuracy must not exceed 1"));
            Assert.That(json, Does.Contain("UpdateFrequency"));
            Assert.That(json, Does.Contain("duplicates another capability"));
            Assert.That(json, Does.Contain("SourceComponentID does not identify"));
        }

        [Test]
        public void RigRead_MapsPreviouslyStoredMudPumpLinerScalarsToOnePerformanceRow()
        {
            Guid id = Guid.NewGuid();
            Rig rig = CreateRig(id, "stored-liner-shape");
            rig.MudPumpList = [new MudPump { Name = "Mud pump 1" }];
            Assert.That(_controller.PostRig(rig), Is.TypeOf<OkResult>());

            using (SqliteConnection connection = _connectionManager.GetConnection()!)
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = """
                    UPDATE RigTable SET data=json_set(data,
                      '$.MudPumpList[0].LinerId', 0.1524,
                      '$.MudPumpList[0].PumpDisplacement', 0.02,
                      '$.MudPumpList[0].MaxLimitOperatingFlowRate', 0.04,
                      '$.MudPumpList[0].MaxLimitOperatingPressure', 30000000)
                    WHERE json_extract(MetaInfo,'$.ID')=$id
                    """;
                command.Parameters.AddWithValue("$id", id.ToString());
                Assert.That(command.ExecuteNonQuery(), Is.EqualTo(1));
            }

            RigReadResponse read = AssertOk<RigReadResponse>(_controller.GetRigById(id).Result);
            MudPumpLinerConfiguration row = read.MudPumpList!.Single().LinerConfigurations!.Single();
            Assert.That(row.LinerInnerDiameter, Is.EqualTo(0.1524));
            Assert.That(row.DisplacementPerStroke, Is.EqualTo(0.02));
            Assert.That(row.MaximumVolumetricFlowRate, Is.EqualTo(0.04));
            Assert.That(row.MaximumDischargePressure, Is.EqualTo(30_000_000));
        }

        [Test]
        public void RigRead_DoesNotCreateLinerConfigurationFromIncompleteHistoricalScalars()
        {
            Guid id = Guid.NewGuid();
            Rig rig = CreateRig(id, "incomplete-stored-liner-shape");
            rig.MudPumpList = [new MudPump { Name = "Mud pump 1", MaxLimitDesignPressure = 35_000_000 }];
            Assert.That(_controller.PostRig(rig), Is.TypeOf<OkResult>());

            using (SqliteConnection connection = _connectionManager.GetConnection()!)
            using (SqliteCommand command = connection.CreateCommand())
            {
                command.CommandText = """
                    UPDATE RigTable SET data=json_set(data,
                      '$.MudPumpList[0].LinerId', 0.127,
                      '$.MudPumpList[0].MaxLimitOperatingFlowRate', 0.025)
                    WHERE json_extract(MetaInfo,'$.ID')=$id
                    """;
                command.Parameters.AddWithValue("$id", id.ToString());
                Assert.That(command.ExecuteNonQuery(), Is.EqualTo(1));
            }

            RigReadResponse read = AssertOk<RigReadResponse>(_controller.GetRigById(id).Result);
            Assert.That(read.MudPumpList!.Single().LinerConfigurations, Is.Null.Or.Empty);
        }

        private static Rig CreateRig(Guid id, string name)
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            return new Rig
            {
                MetaInfo = new MetaInfo { ID = id },
                Name = name,
                Description = $"Description for {name}",
                CreationDate = now,
                LastModificationDate = now,
                IsFixedPlatform = true,
                ClusterID = Guid.NewGuid(),
                RigType = RigType.PlatformRig,
                OperatingEnvironment = RigEnvironment.Offshore,
                MobilityType = RigMobilityType.Fixed,
                MainRigMast = new RigMast
                {
                    Name = $"Mast for {name}"
                }
            };
        }

        private static T AssertOk<T>(IActionResult? actionResult)
        {
            Assert.That(actionResult, Is.TypeOf<OkObjectResult>());
            object? value = ((OkObjectResult)actionResult!).Value;
            Assert.That(value, Is.TypeOf<T>());
            return (T)value!;
        }

        private static void ResetRigManagerSingleton()
        {
            FieldInfo? instanceField = typeof(RigManager).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic);
            instanceField?.SetValue(null, null);
        }

        private static void DeleteTestDatabaseFile(string databaseFilePath)
        {
            foreach (string path in new[] { databaseFilePath, $"{databaseFilePath}-shm", $"{databaseFilePath}-wal" })
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
