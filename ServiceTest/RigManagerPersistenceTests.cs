using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Rig.Service.Managers;
using RigModel = OSDC.Drilling.Rig.Model.Rig;

namespace OSDC.Drilling.Rig.ServiceTest;

[TestFixture]
public sealed class RigManagerPersistenceTests
{
    [Test]
    public void Create_accepts_apostrophes_and_replaces_caller_timestamps()
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, $"RigPersistence_{Guid.NewGuid():N}.db");
        using ILoggerFactory loggers = LoggerFactory.Create(builder => builder.ClearProviders());
        try
        {
            var connections = new SqlConnectionManager($"Data Source={path};Pooling=False",
                loggers.CreateLogger<SqlConnectionManager>());
            ConstructorInfo constructor = typeof(RigManager).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic, null,
                [typeof(ILogger<RigManager>), typeof(SqlConnectionManager)], null)!;
            var manager = (RigManager)constructor.Invoke([loggers.CreateLogger<RigManager>(), connections]);
            var rig = new RigModel
            {
                MetaInfo = new MetaInfo { ID = Guid.NewGuid() },
                Name = "U1's rig",
                Description = "It's a valid description",
                CreationDate = DateTimeOffset.UnixEpoch,
                LastModificationDate = DateTimeOffset.UnixEpoch
            };

            DateTimeOffset before = DateTimeOffset.UtcNow;
            Assert.That(manager.AddRig(rig), Is.True);
            DateTimeOffset after = DateTimeOffset.UtcNow;
            RigModel stored = manager.GetRigById(rig.MetaInfo.ID)!;

            Assert.Multiple(() =>
            {
                Assert.That(stored.Name, Is.EqualTo(rig.Name));
                Assert.That(stored.Description, Is.EqualTo(rig.Description));
                Assert.That(stored.CreationDate, Is.InRange(before, after));
                Assert.That(stored.LastModificationDate, Is.EqualTo(stored.CreationDate));
            });
        }
        finally
        {
            SqliteConnection.ClearAllPools();
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
