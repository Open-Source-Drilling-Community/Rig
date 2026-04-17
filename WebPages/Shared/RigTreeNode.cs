namespace NORCE.Drilling.Rig.WebPages.Shared;

public sealed class RigTreeNode
{
    public string Key { get; init; } = string.Empty;
    public string? ParentKey { get; init; }
    public string Label { get; init; } = string.Empty;
    public object? Value { get; set; }
    public Type ValueType { get; init; } = typeof(object);
    public bool IsRoot { get; init; }
    public bool IsList { get; init; }
    public bool IsListItem { get; init; }
    public int Depth { get; init; }
    public List<RigTreeNode> Children { get; } = new();
    public Func<string>? CreateAction { get; init; }
    public Func<string>? ClearAction { get; init; }
    public Func<string>? AddItemAction { get; init; }
    public Func<string>? DeleteItemAction { get; init; }

    public bool Exists => IsRoot || Value != null;
    public bool CanCreate => CreateAction != null;
    public bool CanClear => ClearAction != null;
    public bool CanAddItem => AddItemAction != null;
    public bool CanDeleteItem => DeleteItemAction != null;
}
