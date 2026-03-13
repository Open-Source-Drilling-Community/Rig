using System.Collections;
using System.Reflection;
using RigShared = NORCE.Drilling.Rig.ModelShared;

namespace WebApp.Shared;

public static class RigTreeBuilder
{
    public static RigTreeNode Build(object root, Type rootType, string label)
    {
        RigTreeNode node = new()
        {
            Key = label,
            Label = label,
            Value = root,
            ValueType = rootType,
            IsRoot = true,
            Depth = 0
        };

        BuildObjectChildren(node, root, rootType, node.Key, node.Depth + 1, () => root);
        return node;
    }

    public static RigTreeNode? FindNode(RigTreeNode? node, string? key)
    {
        if (node == null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(key) || node.Key == key)
        {
            return node;
        }

        foreach (RigTreeNode child in node.Children)
        {
            RigTreeNode? match = FindNode(child, key);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static void BuildObjectChildren(RigTreeNode parentNode, object? instance, Type instanceType, string parentKey, int depth, Func<object?>? ensureInstance)
    {
        if (instanceType == typeof(RigShared.Rig))
        {
            BuildRigChildren(parentNode, instance as RigShared.Rig, parentKey, depth, ensureInstance);
            return;
        }

        if (instanceType == typeof(RigShared.RigMast))
        {
            BuildRigMastChildren(parentNode, instance as RigShared.RigMast, parentKey, depth, ensureInstance);
            return;
        }

        if (instanceType == typeof(RigShared.HoistingSystem))
        {
            BuildHoistingSystemChildren(parentNode, instance as RigShared.HoistingSystem, parentKey, depth, ensureInstance);
            return;
        }

        foreach (PropertyInfo property in instanceType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!property.CanRead || !property.CanWrite || property.GetIndexParameters().Length != 0)
            {
                continue;
            }

            Type propertyType = property.PropertyType;
            Type coreType = GetCoreType(propertyType);

            if (TryGetListItemType(propertyType, out Type? itemType))
            {
                IList? listValue = instance == null ? null : property.GetValue(instance) as IList;
                RigTreeNode listNode = new()
                {
                    Key = parentKey + "." + property.Name,
                    ParentKey = parentKey,
                    Label = DataUtils.GetDisplayName(property.Name),
                    Value = listValue,
                    ValueType = itemType!,
                    IsList = true,
                    Depth = depth,
                    AddItemAction = ensureInstance == null ? null : () => AddListItem(ensureInstance, property, itemType!, parentKey + "." + property.Name),
                    ClearAction = ensureInstance == null ? null : () =>
                    {
                        object? owner = ensureInstance();
                        property.SetValue(owner, null);
                        return parentKey + "." + property.Name;
                    }
                };

                if (listValue != null)
                {
                    for (int i = 0; i < listValue.Count; i++)
                    {
                        int index = i;
                        object? item = listValue[index];
                        string itemKey = listNode.Key + "[" + index + "]";
                        RigTreeNode itemNode = new()
                        {
                            Key = itemKey,
                            ParentKey = listNode.Key,
                            Label = BuildListItemLabel(itemType!, item, index),
                            Value = item,
                            ValueType = itemType!,
                            IsListItem = true,
                            Depth = depth + 1,
                            DeleteItemAction = ensureInstance == null ? null : () => RemoveListItem(ensureInstance, property, index, listNode.Key)
                        };

                        if (item != null && IsComplexNodeType(itemType!))
                        {
                            Func<object?>? ensureListItem = ensureInstance == null ? null : () => GetListItem(ensureInstance, property, index);
                            BuildObjectChildren(itemNode, item, itemType!, itemKey, depth + 2, ensureListItem);
                        }

                        listNode.Children.Add(itemNode);
                    }
                }

                parentNode.Children.Add(listNode);
            }
            else if (IsComplexNodeType(coreType))
            {
                object? propertyValue = instance == null ? null : property.GetValue(instance);
                string propertyKey = parentKey + "." + property.Name;
                Func<object?>? ensureChildInstance = ensureInstance == null ? null : () => EnsureObject(ensureInstance, property, coreType);
                RigTreeNode childNode = new()
                {
                    Key = propertyKey,
                    ParentKey = parentKey,
                    Label = DataUtils.GetDisplayName(property.Name),
                    Value = propertyValue,
                    ValueType = coreType,
                    Depth = depth,
                    CreateAction = ensureChildInstance == null ? null : () =>
                    {
                        ensureChildInstance();
                        return propertyKey;
                    },
                    ClearAction = ensureInstance == null ? null : () =>
                    {
                        object? owner = ensureInstance();
                        property.SetValue(owner, null);
                        return propertyKey;
                    }
                };

                BuildObjectChildren(childNode, propertyValue, coreType, propertyKey, depth + 1, ensureChildInstance);

                parentNode.Children.Add(childNode);
            }
        }
    }

    private static void BuildRigChildren(RigTreeNode parentNode, RigShared.Rig? rig, string parentKey, int depth, Func<object?>? ensureInstance)
    {
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.MainRigMast), typeof(RigShared.RigMast), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.AuxiliaryRigMast), typeof(RigShared.RigMast), parentKey, depth, ensureInstance);
        AddListNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.MudPumpList), typeof(RigShared.MudPump), parentKey, depth, ensureInstance);
        AddListNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.CementPumpList), typeof(RigShared.CementPump), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.CementUnit), typeof(RigShared.CementUnit), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.DriveMode), typeof(RigShared.DriveMode), parentKey, depth, ensureInstance);
        AddListNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.MudTankList), typeof(RigShared.MudTank), parentKey, depth, ensureInstance);
        AddListNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.GeneratorList), typeof(RigShared.Generator), parentKey, depth, ensureInstance);
        AddListNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.ShaleShakerList), typeof(RigShared.ShaleShaker), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.AuxSolidsControl), typeof(RigShared.AuxSolidsControl), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.DrillingFluidType), typeof(RigShared.DrillingFluidTypeDescriptor), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.FlowSensor), typeof(RigShared.FlowSensor), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.MeasurementAfm), typeof(RigShared.MeasurementAfm), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.ReturnFlowLine), typeof(RigShared.ReturnFlowLine), parentKey, depth, ensureInstance);
        AddListNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.MudGasSeparatorList), typeof(RigShared.MudGasSeparator), parentKey, depth, ensureInstance);
        AddListNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.DesanderList), typeof(RigShared.Desander), parentKey, depth, ensureInstance);
        AddListNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.DesilterList), typeof(RigShared.Desilter), parentKey, depth, ensureInstance);
        AddListNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.CentrifugeList), typeof(RigShared.Centrifuge), parentKey, depth, ensureInstance);
        AddListNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.DegasserList), typeof(RigShared.Degasser), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.CuttingsTransportSystem), typeof(RigShared.CuttingsTransportSystem), parentKey, depth, ensureInstance);
        AddListNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.CuttingsDryerList), typeof(RigShared.CuttingsDryer), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.PipeDeck), typeof(RigShared.PipeDeck), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.Accumulator), typeof(RigShared.Accumulator), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.BopStack), typeof(RigShared.BopStack), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.FloatValve), typeof(RigShared.FloatValve), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.AutoDriller), typeof(RigShared.AutoDriller), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.MpdController), typeof(RigShared.MpdController), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.MpdControlDevice), typeof(RigShared.MpdControlDevice), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.ContinuousCirculationDevice), typeof(RigShared.ContinuousCirculationDevice), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.DrillingChokeManifold), typeof(RigShared.DrillingChokeManifold), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.SurfaceMpdEquipment), typeof(RigShared.SurfaceMpdEquipment), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.MarineMpdEquipment), typeof(RigShared.MarineMpdEquipment), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.MultiPhaseSeparator), typeof(RigShared.MultiPhaseSeparator), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.FlowRoutingManifold), typeof(RigShared.FlowRoutingManifold), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.DrillstringHeaveCompensator), typeof(RigShared.DrillstringHeaveCompensator), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.DrillingMarineRiser), typeof(RigShared.DrillingMarineRiser), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rig, typeof(RigShared.Rig), nameof(RigShared.Rig.RiserHeaveCompensator), typeof(RigShared.RiserHeaveCompensator), parentKey, depth, ensureInstance);
    }

    private static void BuildRigMastChildren(RigTreeNode parentNode, RigShared.RigMast? rigMast, string parentKey, int depth, Func<object?>? ensureInstance)
    {
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.CatWalk), typeof(RigShared.CatWalk), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.PipeRack), typeof(RigShared.PipeRack), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.CasingDriveSystem), typeof(RigShared.CasingDriveSystem), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.CoilDriveSystem), typeof(RigShared.CoilDriveSystem), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.Derrick), typeof(RigShared.Derrick), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.HoistingSystem), typeof(RigShared.HoistingSystem), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.TorqueTurnSub), typeof(RigShared.TorqueTurnSub), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.RotaryTable), typeof(RigShared.RotaryTable), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.TopDrive), typeof(RigShared.TopDrive), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.Kelly), typeof(RigShared.Kelly), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.IronRoughneck), typeof(RigShared.IronRoughneck), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.CasingTongs), typeof(RigShared.CasingTongs), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.CasingRunningTool), typeof(RigShared.CasingRunningTool), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.StandPipe), typeof(RigShared.StandPipe), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.StandPipeManifold), typeof(RigShared.StandPipeManifold), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.RotaryHose), typeof(RigShared.RotaryHose), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.ChokeManifold), typeof(RigShared.ChokeManifold), parentKey, depth, ensureInstance);
        AddListNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.RigChokeList), typeof(RigShared.RigChoke), parentKey, depth, ensureInstance);
        AddComplexNode(parentNode, rigMast, typeof(RigShared.RigMast), nameof(RigShared.RigMast.Slips), typeof(RigShared.Slips), parentKey, depth, ensureInstance);
    }

    private static void BuildHoistingSystemChildren(RigTreeNode parentNode, RigShared.HoistingSystem? hoistingSystem, string parentKey, int depth, Func<object?>? ensureInstance)
    {
        if (hoistingSystem?.HoistingSystemType == RigShared.HoistingSystemType.Drawworks)
        {
            AddComplexNode(parentNode, hoistingSystem, typeof(RigShared.HoistingSystem), nameof(RigShared.HoistingSystem.Drawworks), typeof(RigShared.Drawworks), parentKey, depth, ensureInstance);
            AddComplexNode(parentNode, hoistingSystem, typeof(RigShared.HoistingSystem), nameof(RigShared.HoistingSystem.CrownBlock), typeof(RigShared.CrownBlock), parentKey, depth, ensureInstance);
            AddComplexNode(parentNode, hoistingSystem, typeof(RigShared.HoistingSystem), nameof(RigShared.HoistingSystem.TravellingBlock), typeof(RigShared.TravellingBlock), parentKey, depth, ensureInstance);
            AddComplexNode(parentNode, hoistingSystem, typeof(RigShared.HoistingSystem), nameof(RigShared.HoistingSystem.DrillLine), typeof(RigShared.DrillLine), parentKey, depth, ensureInstance);
        }
    }

    private static void AddComplexNode(RigTreeNode parentNode, object? instance, Type ownerType, string propertyName, Type propertyType, string parentKey, int depth, Func<object?>? ensureInstance)
    {
        PropertyInfo property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!;
        object? propertyValue = instance == null ? null : property.GetValue(instance);
        string propertyKey = parentKey + "." + propertyName;
        Func<object?>? ensureChildInstance = ensureInstance == null ? null : () => EnsureObject(ensureInstance, property, propertyType);
        RigTreeNode childNode = new()
        {
            Key = propertyKey,
            ParentKey = parentKey,
            Label = DataUtils.GetDisplayName(propertyName),
            Value = propertyValue,
            ValueType = propertyType,
            Depth = depth,
            CreateAction = ensureChildInstance == null ? null : () =>
            {
                ensureChildInstance();
                return propertyKey;
            },
            ClearAction = ensureInstance == null ? null : () =>
            {
                object? owner = ensureInstance();
                property.SetValue(owner, null);
                return propertyKey;
            }
        };

        BuildObjectChildren(childNode, propertyValue, propertyType, propertyKey, depth + 1, ensureChildInstance);
        parentNode.Children.Add(childNode);
    }

    private static void AddListNode(RigTreeNode parentNode, object? instance, Type ownerType, string propertyName, Type itemType, string parentKey, int depth, Func<object?>? ensureInstance)
    {
        PropertyInfo property = ownerType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance)!;
        IList? listValue = instance == null ? null : property.GetValue(instance) as IList;
        string listKey = parentKey + "." + propertyName;
        RigTreeNode listNode = new()
        {
            Key = listKey,
            ParentKey = parentKey,
            Label = DataUtils.GetDisplayName(propertyName),
            Value = listValue,
            ValueType = itemType,
            IsList = true,
            Depth = depth,
            AddItemAction = ensureInstance == null ? null : () => AddListItem(ensureInstance, property, itemType, listKey),
            ClearAction = ensureInstance == null ? null : () =>
            {
                object? owner = ensureInstance();
                property.SetValue(owner, null);
                return listKey;
            }
        };

        if (listValue != null)
        {
            for (int i = 0; i < listValue.Count; i++)
            {
                int index = i;
                object? item = listValue[index];
                string itemKey = listKey + "[" + index + "]";
                RigTreeNode itemNode = new()
                {
                    Key = itemKey,
                    ParentKey = listKey,
                    Label = BuildListItemLabel(itemType, item, index),
                    Value = item,
                    ValueType = itemType,
                    IsListItem = true,
                    Depth = depth + 1,
                    DeleteItemAction = ensureInstance == null ? null : () => RemoveListItem(ensureInstance, property, index, listKey)
                };

                if (item != null && IsComplexNodeType(itemType))
                {
                    Func<object?>? ensureListItem = ensureInstance == null ? null : () => GetListItem(ensureInstance, property, index);
                    BuildObjectChildren(itemNode, item, itemType, itemKey, depth + 2, ensureListItem);
                }

                listNode.Children.Add(itemNode);
            }
        }

        parentNode.Children.Add(listNode);
    }

    private static object? EnsureObject(Func<object?> ensureOwner, PropertyInfo property, Type propertyType)
    {
        object? owner = ensureOwner();
        if (owner == null)
        {
            return null;
        }

        object? instance = property.GetValue(owner);
        if (instance == null)
        {
            instance = CreateInstance(propertyType);
            if (instance != null)
            {
                property.SetValue(owner, instance);
            }
        }

        return instance;
    }

    private static string AddListItem(Func<object?> ensureOwner, PropertyInfo property, Type itemType, string listKey)
    {
        object? owner = ensureOwner();
        if (owner == null)
        {
            return listKey;
        }

        IList? list = property.GetValue(owner) as IList;
        if (list == null)
        {
            Type listType = typeof(List<>).MakeGenericType(itemType);
            list = (IList)Activator.CreateInstance(listType)!;
            property.SetValue(owner, list);
        }

        list.Add(CreateInstance(itemType));
        return listKey + "[" + (list.Count - 1) + "]";
    }

    private static string RemoveListItem(Func<object?> ensureOwner, PropertyInfo property, int index, string listKey)
    {
        object? owner = ensureOwner();
        if (owner != null && property.GetValue(owner) is IList list && index >= 0 && index < list.Count)
        {
            list.RemoveAt(index);
        }

        return listKey;
    }

    private static object? GetListItem(Func<object?> ensureOwner, PropertyInfo property, int index)
    {
        object? owner = ensureOwner();
        if (owner == null)
        {
            return null;
        }

        if (property.GetValue(owner) is IList list && index >= 0 && index < list.Count)
        {
            return list[index];
        }

        return null;
    }

    private static object? CreateInstance(Type type)
    {
        try
        {
            return Activator.CreateInstance(type);
        }
        catch
        {
            return null;
        }
    }

    private static string BuildListItemLabel(Type itemType, object? item, int index)
    {
        if (item != null)
        {
            PropertyInfo? nameProperty = itemType.GetProperty("Name", BindingFlags.Public | BindingFlags.Instance);
            if (nameProperty?.GetValue(item) is string name && !string.IsNullOrWhiteSpace(name))
            {
                return name;
            }
        }

        return DataUtils.GetDisplayName(itemType.Name) + " " + (index + 1);
    }

    private static bool TryGetListItemType(Type type, out Type? itemType)
    {
        itemType = null;
        if (type == typeof(string))
        {
            return false;
        }

        Type? enumerableType = type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>)
            ? type
            : type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IList<>));

        if (enumerableType == null)
        {
            return false;
        }

        itemType = enumerableType.GetGenericArguments()[0];
        return true;
    }

    private static Type GetCoreType(Type type) => Nullable.GetUnderlyingType(type) ?? type;

    private static bool IsComplexNodeType(Type type)
    {
        Type coreType = GetCoreType(type);
        if (coreType == typeof(string) || coreType == typeof(bool) || coreType == typeof(int) || coreType == typeof(long) || coreType == typeof(double) || coreType == typeof(Guid) || coreType == typeof(DateTimeOffset) || coreType.IsEnum)
        {
            return false;
        }

        if (typeof(IEnumerable).IsAssignableFrom(coreType) && coreType != typeof(string))
        {
            return false;
        }

        return coreType.IsClass || (coreType.IsValueType && !coreType.IsPrimitive);
    }
}
