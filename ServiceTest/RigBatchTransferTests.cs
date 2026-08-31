using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Rig.Model;
using OSDC.Drilling.Rig.Service;
using RigModel = OSDC.Drilling.Rig.Model.Rig;

namespace OSDC.Drilling.Rig.ServiceTest;

[TestFixture]
public sealed class RigBatchTransferTests
{
    [Test]
    public void Export_preserves_selected_order_and_includes_photos()
    {
        RigModel first = Rig(Guid.NewGuid(), "First");
        RigModel second = Rig(Guid.NewGuid(), "Second");
        RigBatchPhoto photo = Photo(second.MetaInfo!.ID);

        RigBatchExportOutcome outcome = RigBatchExporter.Create(
            new RigBatchExportRequest { Scope = RigBatchExportScope.Selected, RigIDs = [second.MetaInfo.ID, first.MetaInfo!.ID] },
            [first, second], [], id => id == second.MetaInfo.ID ? [photo] : [], DateTimeOffset.UtcNow);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(outcome.Document!.Rigs.Select(value => value.MetaInfo!.ID),
                Is.EqualTo(new[] { second.MetaInfo.ID, first.MetaInfo.ID }));
            Assert.That(outcome.Document.Photos, Has.Count.EqualTo(1));
        });
    }

    [Test]
    public void Restore_commits_rig_and_validated_photo_together()
    {
        using SqliteConnection connection = OpenDatabase();
        RigModel rig = Rig(Guid.NewGuid(), "Imported rig");
        RigBatchRestoreOutcome outcome = RigBatchRestorer.Restore(connection,
            Request(rig, Photo(rig.MetaInfo!.ID)), DateTimeOffset.UtcNow, []);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(outcome.Response!.CreatedCount, Is.EqualTo(1));
            Assert.That(outcome.Response.RestoredPhotoCount, Is.EqualTo(1));
            Assert.That(Scalar(connection, "SELECT COUNT(*) FROM RigTable"), Is.EqualTo(1));
            Assert.That(Scalar(connection, "SELECT COUNT(*) FROM RigPhotoTable"), Is.EqualTo(1));
        });
    }

    [Test]
    public void Invalid_photo_rolls_back_the_complete_restore()
    {
        using SqliteConnection connection = OpenDatabase();
        RigModel rig = Rig(Guid.NewGuid(), "Must not remain");
        RigBatchPhoto photo = Photo(rig.MetaInfo!.ID);
        photo.ContentBase64 = Convert.ToBase64String([1, 2, 3]);

        RigBatchRestoreOutcome outcome = RigBatchRestorer.Restore(connection,
            Request(rig, photo), DateTimeOffset.UtcNow, []);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.FailureKind, Is.EqualTo(RigBatchRestoreFailureKind.InvalidRequest));
            Assert.That(Scalar(connection, "SELECT COUNT(*) FROM RigTable"), Is.EqualTo(0));
            Assert.That(Scalar(connection, "SELECT COUNT(*) FROM RigPhotoTable"), Is.EqualTo(0));
        });
    }

    [Test]
    public void Restore_can_create_missing_feature_definitions_and_rewrite_assignments()
    {
        using SqliteConnection connection = OpenDatabase();
        Guid sourceCategoryId = Guid.NewGuid(), sourceOptionId = Guid.NewGuid();
        RigModel rig = Rig(Guid.NewGuid(), "Feature rig");
        rig.FeatureAssignments = [new RigFeatureAssignment
        {
            ID = Guid.NewGuid(), FeatureCategoryID = sourceCategoryId, FeatureOptionID = sourceOptionId
        }];
        RigBatchRestoreRequest request = new()
        {
            ConflictPolicy = RigBatchRestoreConflictPolicy.FailIfExists,
            CatalogPolicy = RigBatchCatalogRestorePolicy.MapOrCreateMissing,
            Document = new RigBatchExportDocument
            {
                ExportedAtUtc = DateTimeOffset.UtcNow,
                CatalogDependencies = new RigBatchCatalogDependencies
                {
                    FeatureCategories = [new RigFeatureCategory
                    {
                        MetaInfo = new MetaInfo { ID = sourceCategoryId }, Code = "CUSTOM_CATEGORY", Name = "Custom category",
                        Options = [new RigFeatureOption { ID = sourceOptionId, Code = "CUSTOM_OPTION", Name = "Custom option" }]
                    }]
                },
                ExternalReferences = new(), Rigs = [rig], Photos = []
            }
        };

        RigBatchRestoreOutcome outcome = RigBatchRestorer.Restore(connection, request, DateTimeOffset.UtcNow, []);
        RigModel restored = ReadRig(connection, rig.MetaInfo!.ID);

        Assert.Multiple(() =>
        {
            Assert.That(outcome.IsSuccess, Is.True);
            Assert.That(outcome.Response!.CreatedCatalogDefinitionCount, Is.EqualTo(1));
            Assert.That(outcome.Response.CreatedCatalogOptionCount, Is.EqualTo(1));
            Assert.That(restored.FeatureAssignments!.Single().FeatureCategoryID, Is.Not.EqualTo(sourceCategoryId));
            Assert.That(restored.FeatureAssignments!.Single().FeatureOptionID, Is.Not.EqualTo(sourceOptionId));
            Assert.That(Scalar(connection, "SELECT COUNT(*) FROM RigFeatureCategoryTable"), Is.EqualTo(1));
        });
    }

    private static RigBatchRestoreRequest Request(RigModel rig, RigBatchPhoto photo) => new()
    {
        ConflictPolicy = RigBatchRestoreConflictPolicy.FailIfExists,
        CatalogPolicy = RigBatchCatalogRestorePolicy.MapExisting,
        Document = new RigBatchExportDocument
        {
            ExportedAtUtc = DateTimeOffset.UtcNow,
            CatalogDependencies = new(), ExternalReferences = new(), Rigs = [rig], Photos = [photo]
        }
    };

    private static RigModel Rig(Guid id, string name) => new()
    {
        MetaInfo = new MetaInfo { ID = id }, Name = name, FeatureAssignments = []
    };

    private static RigBatchPhoto Photo(Guid rigId)
    {
        byte[] content = [137, 80, 78, 71, 13, 10, 26, 10];
        return new RigBatchPhoto
        {
            Metadata = new RigPhotoMetadata
            {
                MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, RigID = rigId, FileName = "rig.png",
                ContentType = "image/png", ByteLength = content.Length,
                Sha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant(), IsPrimary = true,
                CreationDate = DateTimeOffset.UtcNow, LastModificationDate = DateTimeOffset.UtcNow
            },
            ContentBase64 = Convert.ToBase64String(content)
        };
    }

    private static SqliteConnection OpenDatabase()
    {
        SqliteConnection connection = new("Data Source=:memory:");
        connection.Open();
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = """
            CREATE TABLE RigTable (MetaInfo text, Name text, Description text, CreationDate text, LastModificationDate text, IsFixedPlatform bool, ClusterID text, data text);
            CREATE UNIQUE INDEX RigTableMetaInfoIdIndex ON RigTable (json_extract(MetaInfo, '$.ID'));
            CREATE TABLE RigFeatureCategoryTable (MetaInfo text, Code text, Name text, IsExclusive bool, HasValidityPeriod bool, IsBuiltIn bool, CreationDate text, LastModificationDate text, data text);
            CREATE UNIQUE INDEX RigFeatureCategoryTableMetaInfoIdIndex ON RigFeatureCategoryTable (json_extract(MetaInfo, '$.ID'));
            CREATE TABLE RigPhotoTable (MetaInfo text, RigID text, DisplayOrder integer, IsPrimary bool, ContentType text, FileName text, ByteLength integer, Sha256 text, CreationDate text, LastModificationDate text, data text, Content blob);
            CREATE UNIQUE INDEX RigPhotoTableMetaInfoIdIndex ON RigPhotoTable (json_extract(MetaInfo, '$.ID'));
            """;
        command.ExecuteNonQuery();
        return connection;
    }

    private static long Scalar(SqliteConnection connection, string sql)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)command.ExecuteScalar()!;
    }

    private static RigModel ReadRig(SqliteConnection connection, Guid id)
    {
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT data FROM RigTable WHERE json_extract(MetaInfo, '$.ID')=$id";
        command.Parameters.AddWithValue("$id", id.ToString());
        return JsonSerializer.Deserialize<RigModel>((string)command.ExecuteScalar()!, JsonSettings.Options)!;
    }
}
