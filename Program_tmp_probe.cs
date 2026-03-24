using System.Reflection;
using OSDC.UnitConversion.DrillingRazorMudComponents;

var t = typeof(MudUnitAndReferenceChoiceTag);
foreach (var f in t.GetFields(BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Public|BindingFlags.Static).OrderBy(x=>x.Name))
    Console.WriteLine($"FIELD {f.Name} : {f.FieldType.FullName}");
foreach (var p in t.GetProperties(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly).OrderBy(x=>x.Name))
    Console.WriteLine($"PROP {p.Name} : {p.PropertyType.FullName}");
