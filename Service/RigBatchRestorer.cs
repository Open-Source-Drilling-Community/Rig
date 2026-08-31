using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Data.Sqlite;
using OSDC.Drilling.Rig.Model;
using OSDC.Drilling.Rig.Service.Managers;
using OSDC.DotnetLibraries.General.DataManagement;

namespace OSDC.Drilling.Rig.Service;

public enum RigBatchRestoreFailureKind { None, InvalidRequest, Conflict, StorageFailure }
public sealed class RigBatchRestoreOutcome
{
    public RigBatchRestoreResponse? Response { get; init; }
    public RigBatchErrorEnvelope? Error { get; init; }
    public RigBatchRestoreFailureKind FailureKind { get; init; }
    public bool IsSuccess => Response is not null && FailureKind == RigBatchRestoreFailureKind.None;
}

public static class RigBatchRestorer
{
    public static RigBatchRestoreOutcome Restore(SqliteConnection connection, RigBatchRestoreRequest? request,
        DateTimeOffset restoredAtUtc, IReadOnlyList<RigBatchExternalReferenceMapping> externalMappings)
    {
        List<RigBatchError> errors = ValidateRequest(request);
        if (errors.Count != 0) return Failure(RigBatchRestoreFailureKind.InvalidRequest,
            "invalid_batch_restore_request", "The rig batch-restore request is invalid. No changes were made.", errors);

        using SqliteTransaction transaction = connection.BeginTransaction();
        try
        {
            RigBatchExportDocument document = request!.Document!;
            List<Model.Rig> rigs = Clone(document.Rigs);
            Dictionary<Guid, Guid> clusterMappings = externalMappings.ToDictionary(value => value.SourceID, value => value.LocalID);
            foreach (Model.Rig rig in rigs)
                if (rig.ClusterID is Guid clusterId && clusterMappings.TryGetValue(clusterId, out Guid localId)) rig.ClusterID = localId;

            List<RigFeatureCategory> localCategories = ReadCategories(connection, transaction);
            List<RigBatchCatalogMapping> mappings = [];
            Dictionary<(Guid Category, Guid Option), (Guid Category, Guid Option)> optionMappings = [];
            HashSet<RigFeatureCategory> dirtyCategories = [];
            int createdDefinitions = 0, createdOptions = 0;
            bool createMissing = request.CatalogPolicy == RigBatchCatalogRestorePolicy.MapOrCreateMissing;
            foreach (RigFeatureCategory source in document.CatalogDependencies.FeatureCategories)
                ResolveCategory(source, localCategories, createMissing, restoredAtUtc, mappings, optionMappings,
                    dirtyCategories, errors, ref createdDefinitions, ref createdOptions);
            if (errors.Count != 0) { transaction.Rollback(); return Failure(RigBatchRestoreFailureKind.Conflict,
                "catalog_mapping_failed", "Rig feature definitions could not be mapped. No changes were made.", errors); }

            foreach ((Model.Rig rig, int rigIndex) in rigs.Select((value, index) => (value, index)))
            {
                foreach (RigFeatureAssignment assignment in rig.FeatureAssignments ?? [])
                {
                    if (!optionMappings.TryGetValue((assignment.FeatureCategoryID, assignment.FeatureOptionID), out var mapping))
                        errors.Add(Error(rigIndex, "Rigs.FeatureAssignments", "catalog_mapping_missing", "A feature assignment could not be mapped."));
                    else { assignment.FeatureCategoryID = mapping.Category; assignment.FeatureOptionID = mapping.Option; }
                }
                foreach (string validation in RigDefinitionValidator.Validate(rig))
                    errors.Add(Error(rigIndex, "Rigs", "invalid_rig", validation));
            }
            if (errors.Count != 0) { transaction.Rollback(); return Failure(RigBatchRestoreFailureKind.InvalidRequest,
                "invalid_rig", "One or more rigs are invalid. No changes were made.", errors); }

            HashSet<Guid> existing = rigs.Select(value => value.MetaInfo!.ID)
                .Where(id => RowExists(connection, transaction, "RigTable", id)).ToHashSet();
            if (existing.Count != 0 && request.ConflictPolicy == RigBatchRestoreConflictPolicy.FailIfExists)
            {
                transaction.Rollback();
                return Failure(RigBatchRestoreFailureKind.Conflict, "rig_already_exists",
                    "One or more rig UUIDs already exist. No changes were made.", existing.Order().Select(id => Error(null, "Rigs.MetaInfo.ID", "uuid_conflict", $"Rig UUID '{id}' already exists.")).ToList());
            }

            SaveCategories(connection, transaction, dirtyCategories);
            SaveRigs(connection, transaction, rigs, request.ConflictPolicy);
            int photoCount = SavePhotos(connection, transaction, document.Photos, rigs.Select(value => value.MetaInfo!.ID).ToHashSet(), errors);
            if (errors.Count != 0) { transaction.Rollback(); return Failure(RigBatchRestoreFailureKind.InvalidRequest,
                "invalid_photo", "One or more rig photos are invalid. No changes were made.", errors); }
            transaction.Commit();
            return new RigBatchRestoreOutcome
            {
                Response = new RigBatchRestoreResponse
                {
                    RestoredAtUtc = restoredAtUtc.ToUniversalTime(), CreatedCount = rigs.Count - existing.Count,
                    ReplacedCount = existing.Count, RestoredPhotoCount = photoCount,
                    CreatedCatalogDefinitionCount = createdDefinitions, CreatedCatalogOptionCount = createdOptions,
                    CatalogMappings = mappings, ExternalReferenceMappings = externalMappings.ToList(),
                    RigIDs = rigs.Select(value => value.MetaInfo!.ID).ToList()
                }
            };
        }
        catch (Exception)
        {
            try { transaction.Rollback(); } catch { }
            return StorageFailure("The rig batch restore could not be committed.");
        }
    }

    public static RigBatchRestoreOutcome StorageFailure(string message) => Failure(RigBatchRestoreFailureKind.StorageFailure,
        "rig_restore_failed", message, [Error(null, "Document", "storage_failure", "No restore changes were committed.")]);

    public static List<RigBatchError> ValidateRequest(RigBatchRestoreRequest? request)
    {
        if (request is null) return [Error(null, "Request", "required", "A batch-restore request is required.")];
        List<RigBatchError> errors = [];
        if (request.ConflictPolicy is not RigBatchRestoreConflictPolicy.FailIfExists and not RigBatchRestoreConflictPolicy.ReplaceExisting)
            errors.Add(Error(null, "ConflictPolicy", "invalid_conflict_policy", "ConflictPolicy must be FailIfExists or ReplaceExisting."));
        if (request.CatalogPolicy is not RigBatchCatalogRestorePolicy.MapExisting and not RigBatchCatalogRestorePolicy.MapOrCreateMissing)
            errors.Add(Error(null, "CatalogPolicy", "invalid_catalog_policy", "CatalogPolicy must be MapExisting or MapOrCreateMissing."));
        RigBatchExportDocument? document = request.Document;
        if (document is null) { errors.Add(Error(null, "Document", "required", "A batch-export document is required.")); return errors; }
        if (document.FormatIdentifier != RigBatchExportDocument.CurrentFormatIdentifier) errors.Add(Error(null, "Document.FormatIdentifier", "unsupported_format", $"FormatIdentifier must be '{RigBatchExportDocument.CurrentFormatIdentifier}'."));
        if (document.SchemaVersion != RigBatchExportDocument.CurrentSchemaVersion) errors.Add(Error(null, "Document.SchemaVersion", "unsupported_schema_version", $"SchemaVersion must be {RigBatchExportDocument.CurrentSchemaVersion}."));
        if (document.ExportedAtUtc == default || document.ExportedAtUtc.Offset != TimeSpan.Zero) errors.Add(Error(null, "Document.ExportedAtUtc", "invalid_export_timestamp", "ExportedAtUtc must be a non-default UTC timestamp."));
        if (document.CatalogDependencies is null) errors.Add(Error(null, "Document.CatalogDependencies", "required", "CatalogDependencies is required."));
        if (document.ExternalReferences is null) errors.Add(Error(null, "Document.ExternalReferences", "required", "ExternalReferences is required."));
        if (document.CatalogDependencies?.FeatureCategories is null) errors.Add(Error(null, "Document.CatalogDependencies.FeatureCategories", "required", "FeatureCategories is required."));
        if (document.ExternalReferences?.Clusters is null) errors.Add(Error(null, "Document.ExternalReferences.Clusters", "required", "Clusters is required."));
        if (document.Photos is null) errors.Add(Error(null, "Document.Photos", "required", "Photos is required; use an empty array when no photographs are present."));
        if (document.Rigs is null || document.Rigs.Count == 0) { errors.Add(Error(null, "Document.Rigs", "required", "At least one rig is required.")); return errors; }
        HashSet<Guid> ids = [];
        for (int index = 0; index < document.Rigs.Count; index++)
        {
            Guid? id = document.Rigs[index]?.MetaInfo?.ID;
            if (id is null || id == Guid.Empty) errors.Add(Error(index, "Document.Rigs.MetaInfo.ID", "empty_uuid", "Every rig must have a non-empty UUID."));
            else if (!ids.Add(id.Value)) errors.Add(Error(index, "Document.Rigs.MetaInfo.ID", "duplicate_uuid", $"Rig UUID '{id}' occurs more than once."));
        }
        ValidateDependencies(document, errors);
        ValidateExternalReferences(document, errors);
        return errors;
    }

    private static void ValidateDependencies(RigBatchExportDocument document, List<RigBatchError> errors)
    {
        Dictionary<Guid, HashSet<Guid>> available = [];
        foreach ((RigFeatureCategory? category, int categoryIndex) in (document.CatalogDependencies?.FeatureCategories ?? []).Select((value, index) => (value, index)))
        {
            if (category is null) { errors.Add(Error(categoryIndex, "Document.CatalogDependencies.FeatureCategories", "required", "Feature category entries cannot be null.")); continue; }
            Guid id = category.MetaInfo?.ID ?? Guid.Empty;
            if (id == Guid.Empty || available.ContainsKey(id)) { errors.Add(Error(null, "Document.CatalogDependencies.FeatureCategories", "invalid_catalog_uuid", "Feature category UUIDs must be non-empty and unique.")); continue; }
            if (string.IsNullOrWhiteSpace(category.Name)) errors.Add(Error(null, "Document.CatalogDependencies.FeatureCategories.Name", "required", "Feature category names are required."));
            HashSet<Guid> optionIds = [];
            foreach ((RigFeatureOption? option, int optionIndex) in (category.Options ?? []).Select((value, index) => (value, index)))
            {
                if (option is null) { errors.Add(Error(optionIndex, "Document.CatalogDependencies.FeatureCategories.Options", "required", "Feature option entries cannot be null.")); continue; }
                if (option.ID == Guid.Empty || !optionIds.Add(option.ID)) errors.Add(Error(null, "Document.CatalogDependencies.FeatureCategories.Options", "invalid_option_uuid", "Feature option UUIDs must be non-empty and unique."));
                if (string.IsNullOrWhiteSpace(option.Name)) errors.Add(Error(optionIndex, "Document.CatalogDependencies.FeatureCategories.Options.Name", "required", "Feature option names are required."));
            }
            available[id] = optionIds;
        }
        for (int index = 0; index < document.Rigs.Count; index++)
            foreach ((RigFeatureAssignment? assignment, int assignmentIndex) in (document.Rigs[index]?.FeatureAssignments ?? []).Select((value, itemIndex) => (value, itemIndex)))
                if (assignment is null)
                    errors.Add(Error(index, $"Document.Rigs.FeatureAssignments[{assignmentIndex}]", "required", "Feature assignment entries cannot be null."));
                else if (!available.TryGetValue(assignment.FeatureCategoryID, out HashSet<Guid>? options) || !options.Contains(assignment.FeatureOptionID))
                    errors.Add(Error(index, "Document.Rigs.FeatureAssignments", "catalog_dependency_missing", "A referenced feature category or option is absent from CatalogDependencies."));
    }

    private static void ValidateExternalReferences(RigBatchExportDocument document, List<RigBatchError> errors)
    {
        HashSet<Guid> manifest = [];
        foreach ((RigBatchExternalReference? value, int referenceIndex) in (document.ExternalReferences?.Clusters ?? []).Select((value, index) => (value, index)))
        {
            if (value is null) { errors.Add(Error(referenceIndex, "Document.ExternalReferences.Clusters", "required", "Cluster reference entries cannot be null.")); continue; }
            if (value.SourceID == Guid.Empty || !manifest.Add(value.SourceID)) errors.Add(Error(null, "Document.ExternalReferences.Clusters.SourceID", "invalid_uuid", "Cluster source UUIDs must be non-empty and unique."));
            if (string.IsNullOrWhiteSpace(value.Name)) errors.Add(Error(null, "Document.ExternalReferences.Clusters.Name", "required", "Cluster names are required."));
        }
        for (int index = 0; index < document.Rigs.Count; index++)
            if (document.Rigs[index]?.ClusterID is Guid id)
            {
                if (id == Guid.Empty)
                    errors.Add(Error(index, "Document.Rigs.ClusterID", "empty_uuid", "ClusterID must be null or a non-empty UUID."));
                else if (!manifest.Contains(id))
                    errors.Add(Error(index, "Document.Rigs.ClusterID", "external_reference_manifest_missing", $"Cluster UUID '{id}' is absent from ExternalReferences.Clusters."));
            }
    }

    private static void ResolveCategory(RigFeatureCategory source, List<RigFeatureCategory> locals, bool createMissing,
        DateTimeOffset now, List<RigBatchCatalogMapping> mappings,
        Dictionary<(Guid, Guid), (Guid Category, Guid Option)> optionMappings, HashSet<RigFeatureCategory> dirty,
        List<RigBatchError> errors, ref int createdDefinitions, ref int createdOptions)
    {
        Guid sourceId = source.MetaInfo!.ID;
        RigFeatureCategory? local = locals.SingleOrDefault(value => value.MetaInfo?.ID == sourceId);
        string sourceKey = Normalize(source.Code ?? source.Name);
        if (local is null)
        {
            List<RigFeatureCategory> matches = locals.Where(value => Normalize(value.Code ?? value.Name) == sourceKey).ToList();
            if (matches.Count > 1) { errors.Add(Error(null, "CatalogDependencies.FeatureCategories", "ambiguous_catalog_match", $"More than one local category matches '{source.Name}'.")); return; }
            local = matches.SingleOrDefault();
        }
        string resolution = local?.MetaInfo?.ID == sourceId ? "ExactUUID" : "NormalizedCodeOrName";
        if (local is null)
        {
            if (!createMissing) { errors.Add(Error(null, "CatalogDependencies.FeatureCategories", "catalog_definition_missing", $"No compatible local category exists for '{source.Name}'.")); return; }
            local = new RigFeatureCategory
            {
                MetaInfo = new MetaInfo { ID = Guid.NewGuid() }, Code = source.Code, Name = source.Name,
                Description = source.Description, IsExclusive = source.IsExclusive, HasValidityPeriod = source.HasValidityPeriod,
                IsBuiltIn = false, IsDeprecated = source.IsDeprecated, Options = [], CreationDate = now, LastModificationDate = now
            };
            locals.Add(local); dirty.Add(local); createdDefinitions++; resolution = "Created";
        }
        else if (local.IsExclusive != source.IsExclusive || local.HasValidityPeriod != source.HasValidityPeriod)
        { errors.Add(Error(null, "CatalogDependencies.FeatureCategories", "catalog_semantic_conflict", $"Local category '{local.Name}' has incompatible assignment semantics.")); return; }
        mappings.Add(new RigBatchCatalogMapping { Name = source.Name ?? string.Empty, SourceID = sourceId, LocalID = local.MetaInfo!.ID, Resolution = resolution });

        foreach (RigFeatureOption sourceOption in source.Options ?? [])
        {
            RigFeatureOption? localOption = (local.Options ?? []).SingleOrDefault(value => value.ID == sourceOption.ID);
            if (localOption is null)
            {
                List<RigFeatureOption> matches = (local.Options ?? []).Where(value => Normalize(value.Code ?? value.Name) == Normalize(sourceOption.Code ?? sourceOption.Name)).ToList();
                if (matches.Count > 1) { errors.Add(Error(null, "CatalogDependencies.FeatureCategories.Options", "ambiguous_catalog_match", $"More than one local option matches '{sourceOption.Name}'.")); continue; }
                localOption = matches.SingleOrDefault();
            }
            if (localOption is null)
            {
                if (!createMissing || local.IsBuiltIn) { errors.Add(Error(null, "CatalogDependencies.FeatureCategories.Options", "catalog_option_missing", $"No compatible local option exists for '{sourceOption.Name}'.")); continue; }
                localOption = new RigFeatureOption { ID = Guid.NewGuid(), Code = sourceOption.Code, Name = sourceOption.Name, Description = sourceOption.Description, IsDeprecated = sourceOption.IsDeprecated };
                (local.Options ??= []).Add(localOption); local.LastModificationDate = now; dirty.Add(local); createdOptions++;
            }
            optionMappings[(sourceId, sourceOption.ID)] = (local.MetaInfo!.ID, localOption.ID);
        }
    }

    private static List<RigFeatureCategory> ReadCategories(SqliteConnection connection, SqliteTransaction transaction)
    {
        using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction; command.CommandText = "SELECT data FROM RigFeatureCategoryTable";
        using SqliteDataReader reader = command.ExecuteReader(); List<RigFeatureCategory> values = [];
        while (reader.Read()) values.Add(JsonSerializer.Deserialize<RigFeatureCategory>(reader.GetString(0), JsonSettings.Options)!);
        return values;
    }

    private static void SaveCategories(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<RigFeatureCategory> values)
    {
        foreach (RigFeatureCategory value in values)
        {
            using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "INSERT INTO RigFeatureCategoryTable (MetaInfo,Code,Name,IsExclusive,HasValidityPeriod,IsBuiltIn,CreationDate,LastModificationDate,data) VALUES ($meta,$code,$name,$exclusive,$validity,$builtIn,$created,$modified,$data) ON CONFLICT(json_extract(MetaInfo,'$.ID')) DO UPDATE SET Code=excluded.Code,Name=excluded.Name,IsExclusive=excluded.IsExclusive,HasValidityPeriod=excluded.HasValidityPeriod,LastModificationDate=excluded.LastModificationDate,data=excluded.data";
            AddCategoryParameters(command, value); command.ExecuteNonQuery();
        }
    }

    private static void AddCategoryParameters(SqliteCommand command, RigFeatureCategory value)
    {
        command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(value.MetaInfo, JsonSettings.Options));
        command.Parameters.AddWithValue("$code", value.Code ?? string.Empty); command.Parameters.AddWithValue("$name", value.Name ?? string.Empty);
        command.Parameters.AddWithValue("$exclusive", value.IsExclusive); command.Parameters.AddWithValue("$validity", value.HasValidityPeriod);
        command.Parameters.AddWithValue("$builtIn", value.IsBuiltIn); command.Parameters.AddWithValue("$created", value.CreationDate?.ToString("O") ?? string.Empty);
        command.Parameters.AddWithValue("$modified", value.LastModificationDate?.ToString("O") ?? string.Empty);
        command.Parameters.AddWithValue("$data", JsonSerializer.Serialize(value, JsonSettings.Options));
    }

    private static bool RowExists(SqliteConnection connection, SqliteTransaction transaction, string table, Guid id)
    {
        using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction;
        command.CommandText = $"SELECT COUNT(*) FROM {table} WHERE json_extract(MetaInfo,'$.ID')=$id";
        command.Parameters.AddWithValue("$id", id.ToString()); return Convert.ToInt64(command.ExecuteScalar()) != 0;
    }

    private static void SaveRigs(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<Model.Rig> rigs, RigBatchRestoreConflictPolicy policy)
    {
        foreach (Model.Rig rig in rigs)
        {
            using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = policy == RigBatchRestoreConflictPolicy.ReplaceExisting
                ? "INSERT INTO RigTable (MetaInfo,Name,Description,CreationDate,LastModificationDate,IsFixedPlatform,ClusterID,data) VALUES ($meta,$name,$description,$created,$modified,$fixed,$cluster,$data) ON CONFLICT(json_extract(MetaInfo,'$.ID')) DO UPDATE SET Name=excluded.Name,Description=excluded.Description,CreationDate=excluded.CreationDate,LastModificationDate=excluded.LastModificationDate,IsFixedPlatform=excluded.IsFixedPlatform,ClusterID=excluded.ClusterID,data=excluded.data"
                : "INSERT INTO RigTable (MetaInfo,Name,Description,CreationDate,LastModificationDate,IsFixedPlatform,ClusterID,data) VALUES ($meta,$name,$description,$created,$modified,$fixed,$cluster,$data)";
            command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(rig.MetaInfo, JsonSettings.Options)); command.Parameters.AddWithValue("$name", (object?)rig.Name ?? DBNull.Value);
            command.Parameters.AddWithValue("$description", (object?)rig.Description ?? DBNull.Value); command.Parameters.AddWithValue("$created", rig.CreationDate?.ToString("O") ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("$modified", rig.LastModificationDate?.ToString("O") ?? (object)DBNull.Value); command.Parameters.AddWithValue("$fixed", rig.IsFixedPlatform);
            command.Parameters.AddWithValue("$cluster", rig.ClusterID?.ToString() ?? (object)DBNull.Value); command.Parameters.AddWithValue("$data", JsonSerializer.Serialize(rig, JsonSettings.Options)); command.ExecuteNonQuery();
        }
    }

    private static int SavePhotos(SqliteConnection connection, SqliteTransaction transaction, IEnumerable<RigBatchPhoto> photos, HashSet<Guid> rigIds, List<RigBatchError> errors)
    {
        foreach (Guid rigId in rigIds)
        { using SqliteCommand clear = connection.CreateCommand(); clear.Transaction = transaction; clear.CommandText = "DELETE FROM RigPhotoTable WHERE RigID=$rig"; clear.Parameters.AddWithValue("$rig", rigId.ToString()); clear.ExecuteNonQuery(); }
        int index = 0, savedCount = 0;
        HashSet<Guid> sourcePhotoIds = [];
        HashSet<Guid> rigsWithPrimaryPhoto = [];
        foreach (RigBatchPhoto photo in photos ?? [])
        {
            if (photo?.Metadata is null)
            {
                errors.Add(Error(index++, "Document.Photos.Metadata", "required", "Photo metadata is required."));
                continue;
            }
            RigPhotoMetadata metadata = JsonSerializer.Deserialize<RigPhotoMetadata>(
                JsonSerializer.Serialize(photo.Metadata, JsonSettings.Options), JsonSettings.Options)!;
            byte[] content;
            try { content = Convert.FromBase64String(photo.ContentBase64 ?? string.Empty); }
            catch (FormatException) { errors.Add(Error(index++, "Document.Photos.ContentBase64", "invalid_base64", "Photo content is not valid base64.")); continue; }
            if (!rigIds.Contains(metadata.RigID)) { errors.Add(Error(index++, "Document.Photos.Metadata.RigID", "rig_not_in_batch", "Photo RigID is not part of this batch.")); continue; }
            Guid sourcePhotoId = metadata.MetaInfo?.ID ?? Guid.Empty;
            if (sourcePhotoId == Guid.Empty || !sourcePhotoIds.Add(sourcePhotoId))
            { errors.Add(Error(index++, "Document.Photos.Metadata.MetaInfo.ID", "invalid_photo_uuid", "Photo UUIDs must be non-empty and unique.")); continue; }
            if (metadata.IsPrimary && !rigsWithPrimaryPhoto.Add(metadata.RigID))
            { errors.Add(Error(index++, "Document.Photos.Metadata.IsPrimary", "multiple_primary_photos", "A rig can have at most one primary photo.")); continue; }
            string hash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
            if (content.Length == 0 || content.LongLength > RigPhotoManager.MaximumBytes || metadata.ByteLength != content.LongLength || !string.Equals(metadata.Sha256, hash, StringComparison.OrdinalIgnoreCase))
            { errors.Add(Error(index++, "Document.Photos", "invalid_photo_content", "Photo length or SHA-256 does not match its metadata.")); continue; }
            string? contentError = RigPhotoManager.ValidateContent(metadata.ContentType ?? string.Empty, content);
            if (contentError is not null)
            { errors.Add(Error(index++, "Document.Photos", contentError, "Photo content type or image signature is invalid.")); continue; }
            metadata.MetaInfo = new MetaInfo { ID = Guid.NewGuid() }; metadata.ByteLength = content.LongLength; metadata.Sha256 = hash;
            metadata.FileName = Path.GetFileName(metadata.FileName ?? string.Empty);
            using SqliteCommand command = connection.CreateCommand(); command.Transaction = transaction;
            command.CommandText = "INSERT INTO RigPhotoTable(MetaInfo,RigID,DisplayOrder,IsPrimary,ContentType,FileName,ByteLength,Sha256,CreationDate,LastModificationDate,data,Content) VALUES($meta,$rig,$ord,$primary,$type,$file,$length,$sha,$created,$modified,$data,$content)";
            command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(metadata.MetaInfo, JsonSettings.Options)); command.Parameters.AddWithValue("$rig", metadata.RigID.ToString()); command.Parameters.AddWithValue("$ord", metadata.DisplayOrder); command.Parameters.AddWithValue("$primary", metadata.IsPrimary);
            command.Parameters.AddWithValue("$type", metadata.ContentType ?? string.Empty); command.Parameters.AddWithValue("$file", metadata.FileName ?? string.Empty); command.Parameters.AddWithValue("$length", metadata.ByteLength); command.Parameters.AddWithValue("$sha", metadata.Sha256);
            command.Parameters.AddWithValue("$created", metadata.CreationDate?.ToString("O") ?? string.Empty); command.Parameters.AddWithValue("$modified", metadata.LastModificationDate?.ToString("O") ?? string.Empty); command.Parameters.AddWithValue("$data", JsonSerializer.Serialize(metadata, JsonSettings.Options)); command.Parameters.AddWithValue("$content", content); command.ExecuteNonQuery(); index++; savedCount++;
        }
        return savedCount;
    }

    private static List<Model.Rig> Clone(List<Model.Rig> values) => JsonSerializer.Deserialize<List<Model.Rig>>(JsonSerializer.Serialize(values, JsonSettings.Options), JsonSettings.Options)!;
    private static string Normalize(string? value) => string.Join(' ', (value ?? string.Empty).Normalize(NormalizationForm.FormKC).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)).ToUpperInvariant();
    private static RigBatchRestoreOutcome Failure(RigBatchRestoreFailureKind kind, string error, string message, List<RigBatchError> errors) => new() { FailureKind = kind, Error = new RigBatchErrorEnvelope { Error = error, Message = message, Errors = errors } };
    private static RigBatchError Error(int? index, string property, string code, string message) => new() { PositionIndex = index, Property = property, Code = code, Message = message };
}
