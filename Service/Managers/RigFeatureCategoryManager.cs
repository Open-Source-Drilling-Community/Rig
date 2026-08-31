using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Rig.Model;
using System.Text.Json;

namespace OSDC.Drilling.Rig.Service.Managers;

public sealed class RigFeatureCategoryManager
{
    private const string TableName = "RigFeatureCategoryTable";
    private readonly ILogger _logger;
    private readonly SqlConnectionManager _connections;
    private readonly object _seedLock = new();
    private bool _seeded;

    public RigFeatureCategoryManager(ILogger logger, SqlConnectionManager connections)
    {
        _logger = logger;
        _connections = connections;
    }

    public List<Guid> GetAllIds() => GetAll().Select(value => value.MetaInfo!.ID).ToList();
    public List<MetaInfo?> GetAllMetaInfo() => GetAll().Select(value => value.MetaInfo).ToList();

    public RigFeatureCategory? GetById(Guid id)
    {
        if (id == Guid.Empty) return null;
        EnsureBuiltIns();
        using SqliteConnection connection = _connections.GetConnection()!;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT data FROM {TableName} WHERE json_extract(MetaInfo, '$.ID') = $id";
        command.Parameters.AddWithValue("$id", id.ToString());
        return command.ExecuteScalar() is string json
            ? JsonSerializer.Deserialize<RigFeatureCategory>(json, JsonSettings.Options)
            : null;
    }

    public List<RigFeatureCategory> GetAll()
    {
        EnsureBuiltIns();
        List<RigFeatureCategory> result = [];
        using SqliteConnection connection = _connections.GetConnection()!;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT data FROM {TableName} ORDER BY IsBuiltIn DESC, Name COLLATE NOCASE";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            RigFeatureCategory? value = JsonSerializer.Deserialize<RigFeatureCategory>(reader.GetString(0), JsonSettings.Options);
            if (value is not null) result.Add(value);
        }
        return result;
    }

    public RigFeatureCategory? CreateCustom(RigFeatureCategory candidate, out string? error)
    {
        error = ValidateCategory(candidate, creating: true);
        if (error is not null) return null;
        EnsureBuiltIns();
        RigFeatureCategory value = Clone(candidate);
        value.MetaInfo = new MetaInfo { ID = Guid.NewGuid() };
        value.IsBuiltIn = false;
        value.IsDeprecated = false;
        value.Code = NormalizeCode(value.Code ?? value.Name!);
        foreach (RigFeatureOption option in value.Options ?? [])
        {
            option.ID = Guid.NewGuid();
            option.Code = NormalizeCode(option.Code ?? option.Name!);
            option.IsBuiltIn = false;
        }
        DateTimeOffset now = DateTimeOffset.UtcNow;
        value.CreationDate = now;
        value.LastModificationDate = now;
        if (CodeExists(value.Code!, null))
        {
            error = $"A rig feature category with code '{value.Code}' already exists.";
            return null;
        }
        Insert(value);
        return value;
    }

    public RigFeatureCategory? UpdateCustom(Guid id, DateTimeOffset expectedModifiedUtc, RigFeatureCategory candidate, out string? error)
    {
        error = null;
        RigFeatureCategory? stored = GetById(id);
        if (stored is null) { error = "not_found"; return null; }
        if (stored.IsBuiltIn) { error = "built_in_immutable"; return null; }
        if (stored.LastModificationDate is null || stored.LastModificationDate.Value.ToUniversalTime() != expectedModifiedUtc.ToUniversalTime())
        { error = "concurrency_conflict"; return null; }
        error = ValidateCategory(candidate, creating: false);
        if (error is not null) return null;

        RigFeatureCategory value = Clone(candidate);
        value.MetaInfo = stored.MetaInfo;
        value.CreationDate = stored.CreationDate;
        value.LastModificationDate = DateTimeOffset.UtcNow;
        value.IsBuiltIn = false;
        value.Code = NormalizeCode(value.Code ?? value.Name!);
        if (CodeExists(value.Code, id)) { error = $"A rig feature category with code '{value.Code}' already exists."; return null; }

        HashSet<Guid> oldOptionIds = (stored.Options ?? []).Select(option => option.ID).ToHashSet();
        foreach (RigFeatureOption option in value.Options ?? [])
        {
            if (option.ID == Guid.Empty) option.ID = Guid.NewGuid();
            else if (!oldOptionIds.Contains(option.ID)) { error = $"Option UUID '{option.ID}' does not belong to this category."; return null; }
            option.Code = NormalizeCode(option.Code ?? option.Name!);
            option.IsBuiltIn = false;
        }
        HashSet<Guid> retained = (value.Options ?? []).Select(option => option.ID).ToHashSet();
        foreach (Guid removed in oldOptionIds.Except(retained))
        {
            if (IsOptionReferenced(id, removed)) { error = $"Option UUID '{removed}' is assigned to at least one rig."; return null; }
        }
        Update(value);
        return value;
    }

    public bool DeleteCustom(Guid id, out string? error)
    {
        error = null;
        RigFeatureCategory? stored = GetById(id);
        if (stored is null) { error = "not_found"; return false; }
        if (stored.IsBuiltIn) { error = "built_in_immutable"; return false; }
        if (IsCategoryReferenced(id)) { error = "category_in_use"; return false; }
        using SqliteConnection connection = _connections.GetConnection()!;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = $"DELETE FROM {TableName} WHERE json_extract(MetaInfo, '$.ID') = $id";
        command.Parameters.AddWithValue("$id", id.ToString());
        return command.ExecuteNonQuery() == 1;
    }

    public List<string> ValidateAssignments(IReadOnlyCollection<RigFeatureAssignment>? assignments)
    {
        if (assignments is null || assignments.Count == 0) return [];
        Dictionary<Guid, RigFeatureCategory> categories = GetAll().ToDictionary(value => value.MetaInfo!.ID);
        List<string> errors = [];
        HashSet<Guid> assignmentIds = [];
        foreach ((RigFeatureAssignment assignment, int index) in assignments.Select((value, index) => (value, index)))
        {
            if (assignment.ID == Guid.Empty || !assignmentIds.Add(assignment.ID)) errors.Add($"FeatureAssignments[{index}].ID must be a unique non-empty UUID.");
            if (!categories.TryGetValue(assignment.FeatureCategoryID, out RigFeatureCategory? category))
            { errors.Add($"FeatureAssignments[{index}].FeatureCategoryID does not identify a stored category."); continue; }
            if (category.IsDeprecated) errors.Add($"FeatureAssignments[{index}] references a deprecated category.");
            RigFeatureOption? option = category.Options?.SingleOrDefault(value => value.ID == assignment.FeatureOptionID);
            if (option is null) errors.Add($"FeatureAssignments[{index}].FeatureOptionID is not an option of the selected category.");
            else if (option.IsDeprecated) errors.Add($"FeatureAssignments[{index}] references a deprecated option.");
            if (!category.HasValidityPeriod && (assignment.FromDate is not null || assignment.ToDate is not null))
                errors.Add($"FeatureAssignments[{index}] supplies a validity period for a category that does not support one.");
            if (assignment.FromDate is not null && assignment.ToDate is not null && assignment.FromDate > assignment.ToDate)
                errors.Add($"FeatureAssignments[{index}].FromDate must not be later than ToDate.");
        }
        foreach (IGrouping<Guid, RigFeatureAssignment> group in assignments.GroupBy(value => value.FeatureCategoryID))
        {
            if (!categories.TryGetValue(group.Key, out RigFeatureCategory? category) || !category.IsExclusive || group.Count() < 2) continue;
            RigFeatureAssignment[] values = group.ToArray();
            for (int left = 0; left < values.Length; left++)
            for (int right = left + 1; right < values.Length; right++)
                if (!category.HasValidityPeriod || PeriodsOverlap(values[left], values[right]))
                    errors.Add($"Feature category '{category.Name}' is exclusive and has overlapping assignments '{values[left].ID}' and '{values[right].ID}'.");
        }
        return errors;
    }

    private static bool PeriodsOverlap(RigFeatureAssignment left, RigFeatureAssignment right) =>
        (left.ToDate is null || right.FromDate is null || left.ToDate >= right.FromDate) &&
        (right.ToDate is null || left.FromDate is null || right.ToDate >= left.FromDate);

    private void EnsureBuiltIns()
    {
        if (_seeded) return;
        lock (_seedLock)
        {
            if (_seeded) return;
            using SqliteConnection connection = _connections.GetConnection()!;
            foreach (RigFeatureCategory value in BuiltIns())
            {
                using SqliteCommand command = CreateWriteCommand(connection, value,
                    $"INSERT OR IGNORE INTO {TableName} (MetaInfo,Code,Name,IsExclusive,HasValidityPeriod,IsBuiltIn,CreationDate,LastModificationDate,data) VALUES ($meta,$code,$name,$exclusive,$validity,$builtIn,$created,$modified,$data)");
                command.ExecuteNonQuery();
            }
            _seeded = true;
        }
    }

    private void Insert(RigFeatureCategory value)
    {
        using SqliteConnection connection = _connections.GetConnection()!;
        using SqliteCommand command = CreateWriteCommand(connection, value,
            $"INSERT INTO {TableName} (MetaInfo,Code,Name,IsExclusive,HasValidityPeriod,IsBuiltIn,CreationDate,LastModificationDate,data) VALUES ($meta,$code,$name,$exclusive,$validity,$builtIn,$created,$modified,$data)");
        command.ExecuteNonQuery();
    }

    private void Update(RigFeatureCategory value)
    {
        using SqliteConnection connection = _connections.GetConnection()!;
        using SqliteCommand command = CreateWriteCommand(connection, value,
            $"UPDATE {TableName} SET MetaInfo=$meta,Code=$code,Name=$name,IsExclusive=$exclusive,HasValidityPeriod=$validity,IsBuiltIn=$builtIn,CreationDate=$created,LastModificationDate=$modified,data=$data WHERE json_extract(MetaInfo, '$.ID')=$id");
        command.Parameters.AddWithValue("$id", value.MetaInfo!.ID.ToString());
        command.ExecuteNonQuery();
    }

    private static SqliteCommand CreateWriteCommand(SqliteConnection connection, RigFeatureCategory value, string sql)
    {
        SqliteCommand command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$meta", JsonSerializer.Serialize(value.MetaInfo, JsonSettings.Options));
        command.Parameters.AddWithValue("$code", value.Code ?? string.Empty);
        command.Parameters.AddWithValue("$name", value.Name ?? string.Empty);
        command.Parameters.AddWithValue("$exclusive", value.IsExclusive);
        command.Parameters.AddWithValue("$validity", value.HasValidityPeriod);
        command.Parameters.AddWithValue("$builtIn", value.IsBuiltIn);
        command.Parameters.AddWithValue("$created", value.CreationDate?.ToString("O") ?? string.Empty);
        command.Parameters.AddWithValue("$modified", value.LastModificationDate?.ToString("O") ?? string.Empty);
        command.Parameters.AddWithValue("$data", JsonSerializer.Serialize(value, JsonSettings.Options));
        return command;
    }

    private bool CodeExists(string code, Guid? except)
    {
        return GetAll().Any(value => value.MetaInfo?.ID != except && string.Equals(value.Code, code, StringComparison.OrdinalIgnoreCase));
    }

    private bool IsCategoryReferenced(Guid categoryId) => ReadRigs().Any(rig => rig.FeatureAssignments?.Any(value => value.FeatureCategoryID == categoryId) == true);
    private bool IsOptionReferenced(Guid categoryId, Guid optionId) => ReadRigs().Any(rig => rig.FeatureAssignments?.Any(value => value.FeatureCategoryID == categoryId && value.FeatureOptionID == optionId) == true);

    private IEnumerable<Model.Rig> ReadRigs()
    {
        using SqliteConnection connection = _connections.GetConnection()!;
        using SqliteCommand command = connection.CreateCommand();
        command.CommandText = "SELECT data FROM RigTable";
        using SqliteDataReader reader = command.ExecuteReader();
        while (reader.Read())
        {
            Model.Rig? rig = JsonSerializer.Deserialize<Model.Rig>(reader.GetString(0), JsonSettings.Options);
            if (rig is not null) yield return rig;
        }
    }

    private static string? ValidateCategory(RigFeatureCategory value, bool creating)
    {
        if (string.IsNullOrWhiteSpace(value.Name)) return "Name is required.";
        if (value.IsBuiltIn) return "Callers cannot create or modify built-in categories.";
        if (value.Options is null || value.Options.Count == 0) return "At least one option is required.";
        if (value.Options.Any(option => string.IsNullOrWhiteSpace(option.Name))) return "Every option requires a name.";
        string[] codes = value.Options.Select(option => NormalizeCode(option.Code ?? option.Name!)).ToArray();
        if (codes.Distinct(StringComparer.OrdinalIgnoreCase).Count() != codes.Length) return "Option codes must be unique within a category.";
        if (creating && value.Options.Any(option => option.IsBuiltIn)) return "Callers cannot create built-in options.";
        return null;
    }

    private static RigFeatureCategory Clone(RigFeatureCategory value) =>
        JsonSerializer.Deserialize<RigFeatureCategory>(JsonSerializer.Serialize(value, JsonSettings.Options), JsonSettings.Options)!;

    private static string NormalizeCode(string value) => string.Join('-', value.Trim().ToLowerInvariant().Split([' ', '_', '/'], StringSplitOptions.RemoveEmptyEntries));

    private static IEnumerable<RigFeatureCategory> BuiltIns()
    {
        DateTimeOffset created = new(2026, 8, 29, 0, 0, 0, TimeSpan.Zero);
        yield return BuiltIn(1, "supported-operations", "Supported operations", true,
            "Operations for which the rig is equipped and approved.", ["Drilling", "Completion", "Workover", "Well intervention", "Plug and abandonment", "Well servicing", "Coiled-tubing operations"], created);
        yield return BuiltIn(2, "advanced-drilling", "Advanced drilling capabilities", true,
            "Specialized drilling techniques supported by the rig.", ["Managed pressure drilling", "Underbalanced drilling", "Dual-gradient drilling", "Riserless mud recovery", "Casing while drilling", "Coiled-tubing drilling", "Extended-reach drilling", "High-pressure/high-temperature drilling"], created);
        yield return BuiltIn(3, "drilling-workflow", "Drilling workflow capabilities", true,
            "Workflow and simultaneous-operation capabilities.", ["Dual activity", "Offline stand building", "Offline casing preparation", "Batch drilling", "Simultaneous operations", "Continuous circulation"], created);
        yield return BuiltIn(4, "automation", "Automation capabilities", true,
            "Technology-neutral drilling and handling automation capabilities.", ["Autodriller", "Automated connection", "Automated tripping", "Automated pipe handling", "Automated stand building", "Automated mud-pump control", "Closed-loop drilling control", "Remote operation", "Remote monitoring"], created);
        yield return BuiltIn(5, "mobility-deployment", "Mobility and deployment capabilities", false,
            "Additional movement and deployment capabilities beyond the fundamental rig type.", ["Walking", "Skidding", "Trailer mounted", "Modular", "Rapid-move", "Heli-transportable", "Rail-mounted", "Self-propelled"], created);
        yield return BuiltIn(6, "environmental-suitability", "Environmental suitability", true,
            "Environmental regimes for which the rig is configured or qualified.", ["Harsh-environment", "Arctic/winterized", "Desert/high-temperature", "Sour-service capable", "Deepwater capable", "Ultra-deepwater capable"], created);
        yield return BuiltIn(7, "energy-emissions", "Energy and emissions capabilities", true,
            "Power integration, energy storage, recovery, and emissions capabilities.", ["Grid connection", "Shore power", "Dual-fuel generation", "Hybrid battery system", "Energy storage", "Regenerative energy", "Waste-heat recovery", "Emissions monitoring", "Low-emission operation"], created);
    }

    private static RigFeatureCategory BuiltIn(int categoryNumber, string code, string name, bool validity, string description, string[] options, DateTimeOffset created)
    {
        return new RigFeatureCategory
        {
            MetaInfo = new MetaInfo { ID = BuiltInId(categoryNumber, 0) }, Code = code, Name = name, Description = description,
            IsExclusive = false, HasValidityPeriod = validity, IsBuiltIn = true, Options = options.Select((option, index) => new RigFeatureOption
            {
                ID = BuiltInId(categoryNumber, index + 1), Code = NormalizeCode(option), Name = option, IsBuiltIn = true
            }).ToList(), CreationDate = created, LastModificationDate = created
        };
    }

    private static Guid BuiltInId(int category, int option) => Guid.Parse($"51000000-0000-4000-8{category:D3}-{option:D12}");
}
