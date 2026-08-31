using OSDC.Drilling.Rig.Model;

namespace OSDC.Drilling.Rig.Service;

public enum RigBatchExportFailureKind { None, InvalidRequest, RigNotFound, StorageFailure }

public sealed class RigBatchExportOutcome
{
    public RigBatchExportDocument? Document { get; init; }
    public RigBatchErrorEnvelope? Error { get; init; }
    public RigBatchExportFailureKind FailureKind { get; init; }
    public bool IsSuccess => Document is not null && FailureKind == RigBatchExportFailureKind.None;
}

public static class RigBatchExporter
{
    public static RigBatchExportOutcome Create(RigBatchExportRequest? request, IEnumerable<Model.Rig?> snapshot,
        IEnumerable<RigFeatureCategory> categories, Func<Guid, IEnumerable<RigBatchPhoto>> photos, DateTimeOffset exportedAtUtc)
    {
        List<RigBatchError> errors = ValidateRequest(request);
        if (errors.Count != 0) return Failure(RigBatchExportFailureKind.InvalidRequest,
            "invalid_batch_export_request", "The rig batch-export request is invalid.", errors);

        Dictionary<Guid, Model.Rig> byId = [];
        int position = 0;
        foreach (Model.Rig? rig in snapshot)
        {
            Guid? id = rig?.MetaInfo?.ID;
            if (rig is null || id is null || id == Guid.Empty || !byId.TryAdd(id.Value, rig))
                return Failure(RigBatchExportFailureKind.StorageFailure, "rig_export_failed",
                    "A stored rig could not be represented in the export.",
                    [Error(position, "Rigs", "invalid_stored_rig", "A stored rig is null, has no UUID, or duplicates another UUID.")]);
            position++;
        }

        List<Model.Rig> selected = [];
        if (request!.Scope == RigBatchExportScope.All)
            selected = byId.OrderBy(pair => pair.Key).Select(pair => pair.Value).ToList();
        else
            for (int index = 0; index < request.RigIDs!.Count; index++)
            {
                Guid id = request.RigIDs[index];
                if (byId.TryGetValue(id, out Model.Rig? rig)) selected.Add(rig);
                else errors.Add(Error(index, "RigIDs", "rig_not_found", $"No stored rig has UUID '{id}'."));
            }
        if (errors.Count != 0) return Failure(RigBatchExportFailureKind.RigNotFound, "rig_not_found",
            "The selected batch could not be exported because one or more rigs do not exist.", errors);

        Dictionary<Guid, RigFeatureCategory> categoryIndex = categories
            .Where(value => value.MetaInfo?.ID is Guid id && id != Guid.Empty)
            .ToDictionary(value => value.MetaInfo!.ID);
        Dictionary<Guid, HashSet<Guid>> required = [];
        foreach ((Model.Rig rig, int rigIndex) in selected.Select((value, index) => (value, index)))
            foreach (RigFeatureAssignment assignment in rig.FeatureAssignments ?? [])
            {
                if (assignment.FeatureCategoryID == Guid.Empty || assignment.FeatureOptionID == Guid.Empty)
                { errors.Add(Error(rigIndex, "Rigs.FeatureAssignments", "invalid_catalog_reference", "Feature category and option UUIDs must be non-empty.")); continue; }
                if (!required.TryGetValue(assignment.FeatureCategoryID, out HashSet<Guid>? options))
                    required.Add(assignment.FeatureCategoryID, options = []);
                options.Add(assignment.FeatureOptionID);
            }

        RigBatchCatalogDependencies dependencies = new();
        foreach ((Guid categoryId, HashSet<Guid> optionIds) in required.OrderBy(pair => pair.Key))
        {
            if (!categoryIndex.TryGetValue(categoryId, out RigFeatureCategory? category))
            { errors.Add(Error(null, "CatalogDependencies.FeatureCategories", "referenced_definition_missing", $"Referenced feature category '{categoryId}' does not exist.")); continue; }
            Dictionary<Guid, RigFeatureOption> options = (category.Options ?? []).ToDictionary(value => value.ID);
            List<RigFeatureOption> selectedOptions = [];
            foreach (Guid optionId in optionIds.Order())
                if (options.TryGetValue(optionId, out RigFeatureOption? option)) selectedOptions.Add(option);
                else errors.Add(Error(null, "CatalogDependencies.FeatureCategories.Options", "referenced_option_missing", $"Referenced option '{optionId}' does not exist in category '{categoryId}'."));
            dependencies.FeatureCategories.Add(new RigFeatureCategory
            {
                MetaInfo = category.MetaInfo, Code = category.Code, Name = category.Name, Description = category.Description,
                IsExclusive = category.IsExclusive, HasValidityPeriod = category.HasValidityPeriod,
                IsBuiltIn = category.IsBuiltIn, IsDeprecated = category.IsDeprecated, Options = selectedOptions,
                CreationDate = category.CreationDate, LastModificationDate = category.LastModificationDate
            });
        }
        if (errors.Count != 0) return Failure(RigBatchExportFailureKind.StorageFailure,
            "rig_export_dependency_missing", "The export could not include every referenced feature definition.", errors);

        return new RigBatchExportOutcome
        {
            Document = new RigBatchExportDocument
            {
                ExportedAtUtc = exportedAtUtc.ToUniversalTime(), CatalogDependencies = dependencies, Rigs = selected,
                Photos = selected.SelectMany(rig => photos(rig.MetaInfo!.ID)).ToList()
            }
        };
    }

    public static RigBatchExportOutcome StorageFailure(string message) => Failure(RigBatchExportFailureKind.StorageFailure,
        "rig_export_failed", message, [Error(null, "Document", "storage_failure", "The export snapshot could not be produced.")]);

    private static List<RigBatchError> ValidateRequest(RigBatchExportRequest? request)
    {
        if (request is null) return [Error(null, "Request", "required", "A batch-export request is required.")];
        List<RigBatchError> errors = [];
        if (request.Scope == RigBatchExportScope.All)
        { if (request.RigIDs is { Count: > 0 }) errors.Add(Error(null, "RigIDs", "forbidden", "RigIDs must be omitted for an All export.")); }
        else if (request.Scope == RigBatchExportScope.Selected)
        {
            if (request.RigIDs is null || request.RigIDs.Count == 0) errors.Add(Error(null, "RigIDs", "required", "Selected export requires at least one UUID."));
            else
            {
                HashSet<Guid> ids = [];
                for (int index = 0; index < request.RigIDs.Count; index++)
                    if (request.RigIDs[index] == Guid.Empty) errors.Add(Error(index, "RigIDs", "empty_uuid", "Rig UUIDs must be non-empty."));
                    else if (!ids.Add(request.RigIDs[index])) errors.Add(Error(index, "RigIDs", "duplicate_uuid", $"Rig UUID '{request.RigIDs[index]}' occurs more than once."));
            }
        }
        else errors.Add(Error(null, "Scope", "invalid_scope", "Scope must be All or Selected."));
        return errors;
    }

    private static RigBatchExportOutcome Failure(RigBatchExportFailureKind kind, string error, string message, List<RigBatchError> errors) =>
        new() { FailureKind = kind, Error = new RigBatchErrorEnvelope { Error = error, Message = message, Errors = errors } };
    private static RigBatchError Error(int? index, string property, string code, string message) =>
        new() { PositionIndex = index, Property = property, Code = code, Message = message };
}
