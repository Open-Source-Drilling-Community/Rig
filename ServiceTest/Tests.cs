using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NORCE.Drilling.Rig.Model;
using NORCE.Drilling.Rig.Service.Controllers;
using NORCE.Drilling.Rig.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;
using System.Reflection;

namespace ServiceTest
{
    public class Tests
    {
        private static readonly string DatabaseFilePath = Path.Combine(SqlConnectionManager.HOME_DIRECTORY, SqlConnectionManager.DATABASE_FILENAME);

        private ILoggerFactory _loggerFactory = null!;
        private SqlConnectionManager _connectionManager = null!;
        private RigController _controller = null!;

        [SetUp]
        public void SetUp()
        {
            _loggerFactory = LoggerFactory.Create(builder => { });
            ResetRigManagerSingleton();
            RecreateDatabaseFile();

            _connectionManager = new SqlConnectionManager(
                $"Data Source={DatabaseFilePath}",
                _loggerFactory.CreateLogger<SqlConnectionManager>());

            _controller = new RigController(_loggerFactory.CreateLogger<RigManager>(), _connectionManager);
        }

        [TearDown]
        public void TearDown()
        {
            ResetRigManagerSingleton();
            _loggerFactory.Dispose();
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
            ActionResult<IEnumerable<Rig?>> actionResult = _controller.GetAllRig();

            List<Rig?> rigs = AssertOk<List<Rig?>>(actionResult.Result);

            Assert.That(rigs, Is.Empty);
        }

        [Test]
        public void GetRigById_ReturnsBadRequest_ForEmptyGuid()
        {
            ActionResult<Rig?> actionResult = _controller.GetRigById(Guid.Empty);

            Assert.That(actionResult.Result, Is.TypeOf<BadRequestResult>());
        }

        [Test]
        public void GetRigById_ReturnsNotFound_WhenRigDoesNotExist()
        {
            ActionResult<Rig?> actionResult = _controller.GetRigById(Guid.NewGuid());

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

            Rig persistedRig = AssertOk<Rig>(_controller.GetRigById(id).Result);
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

            List<Rig?> rigs = AssertOk<List<Rig?>>(_controller.GetAllRig().Result);
            Assert.That(rigs.Any(x => x?.MetaInfo?.ID == id && x.Name == rig.Name), Is.True);
        }

        [Test]
        public void PutRigById_ReturnsBadRequest_WhenPayloadIsNull()
        {
            ActionResult actionResult = _controller.PutRigById(Guid.NewGuid(), null);

            Assert.That(actionResult, Is.TypeOf<BadRequestResult>());
        }

        [Test]
        public void PutRigById_ReturnsBadRequest_WhenRouteIdDoesNotMatchPayloadId()
        {
            Rig rig = CreateRig(Guid.NewGuid(), "mismatch-rig");

            ActionResult actionResult = _controller.PutRigById(Guid.NewGuid(), rig);

            Assert.That(actionResult, Is.TypeOf<BadRequestResult>());
        }

        [Test]
        public void PutRigById_ReturnsNotFound_WhenRigDoesNotExist()
        {
            Guid id = Guid.NewGuid();
            Rig rig = CreateRig(id, "missing-rig");

            ActionResult actionResult = _controller.PutRigById(id, rig);

            Assert.That(actionResult, Is.TypeOf<NotFoundResult>());
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

            ActionResult actionResult = _controller.PutRigById(id, rig);

            Assert.That(actionResult, Is.TypeOf<OkResult>());

            Rig updatedRig = AssertOk<Rig>(_controller.GetRigById(id).Result);
            Assert.That(updatedRig.Name, Is.EqualTo("updated-rig"));
            Assert.That(updatedRig.Description, Is.EqualTo("updated-description"));
            Assert.That(updatedRig.IsFixedPlatform, Is.EqualTo(rig.IsFixedPlatform));
            Assert.That(updatedRig.LastModificationDate, Is.Not.Null);
            Assert.That(updatedRig.LastModificationDate, Is.GreaterThanOrEqualTo(originalLastModification));
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

        private static void RecreateDatabaseFile()
        {
            Directory.CreateDirectory(SqlConnectionManager.HOME_DIRECTORY);

            if (File.Exists(DatabaseFilePath))
            {
                File.Delete(DatabaseFilePath);
            }
        }
    }
}
