using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Writers;
using OSDC.DotnetLibraries.Drilling.SemanticCatalogue;
using OSDC.Drilling.Rig.Model;
using OSDC.Drilling.Rig.Semantics;
using OSDC.Drilling.Rig.Service;
using OSDC.Drilling.Rig.Service.Mcp.Tools;
using OSDC.Drilling.Rig.WebPages.Shared;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OSDC.Drilling.Rig.ServiceTest;

public class SemanticContractTests
{
    private const string Semantic = SemanticMetadata.ExtensionName;
    private const string Bindings = ProviderSemantics.NestedBindingsExtension;

    private static JsonObject Rest(Type type)
    {
        var options = new SchemaGeneratorOptions { SchemaIdSelector = candidate => candidate.FullName! };
        options.SchemaFilters.Add(new SemanticSchemaFilter());
        var generator = new SchemaGenerator(options, new JsonSerializerDataContractResolver(new JsonSerializerOptions()));
        var repository = new SchemaRepository();
        generator.GenerateSchema(type, repository);
        using var text = new StringWriter();
        var writer = new OpenApiJsonWriter(text);
        repository.Schemas[type.FullName!].SerializeAsV3(writer);
        writer.Flush();
        return JsonNode.Parse(text.ToString())!.AsObject();
    }

    [Test]
    public void PublishedCatalogueVersionIsTheProviderSourceOfTruth()
    {
        Assert.That(SemanticCatalogue.Default.Document.Version, Is.EqualTo("0.15.0"));
        Assert.That(SemanticCatalogue.Default.Get(Concepts.Elevation).Status, Is.EqualTo(CurationStatus.Reviewed));
        Assert.That(SemanticCatalogue.Default.Get(Concepts.AbsolutePressureRating).Status, Is.EqualTo(CurationStatus.Reviewed));
    }

    [Test]
    public void StandpipeElevationsHaveTheSameRestMcpAndWebBinding()
    {
        var rest = Rest(typeof(StandPipe));
        var mcp = McpToolArgumentHelpers.CreateRigSchema()["$defs"]!["StandPipe"]!;
        foreach (string property in new[] { nameof(StandPipe.PressureMeasurementElevation), nameof(StandPipe.MudHoseHangingPointElevation) })
        {
            JsonNode restMetadata = rest["properties"]![property]![Semantic]!;
            JsonNode mcpMetadata = mcp["properties"]![property]![Semantic]!;
            Assert.That(JsonNode.DeepEquals(restMetadata, mcpMetadata), Is.True, property);
            Assert.That(restMetadata["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.Elevation));
            Assert.That(restMetadata["reference"]!.GetValue<string>(), Is.EqualTo(Concepts.DrillFloorUpward));
            Assert.That(restMetadata["catalogueVersion"]!.GetValue<string>(), Is.EqualTo("0.15.0"));
            Assert.That(DataUtils.InferQuantity(typeof(StandPipe), property), Is.EqualTo("LengthStandard"));
        }
    }

    [Test]
    public void ApprovedAmbiguousQuantitiesReplaceNameBasedGuessing()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DataUtils.InferQuantity(typeof(TopDrive), nameof(TopDrive.Weight)), Is.EqualTo("MassDrilling"));
            Assert.That(DataUtils.InferQuantity(typeof(TopDrive), nameof(TopDrive.ProportionalGain)), Is.EqualTo("ProportionStandard"));
            Assert.That(DataUtils.InferQuantity(typeof(Generator), nameof(Generator.Speed)), Is.EqualTo("AngularVelocityDrilling"));
            Assert.That(DataUtils.InferQuantity(typeof(DrillstringHeaveCompensator), nameof(DrillstringHeaveCompensator.CompensatorCapacity)), Is.EqualTo("ForceDrilling"));
        });
        Assert.That(ProviderSemantics.ForProperty(typeof(TopDrive).GetProperty(nameof(TopDrive.Weight))!)!["concept"]!.GetValue<string>(),
            Is.EqualTo(Concepts.EquipmentMass));
        Assert.That(ProviderSemantics.ForProperty(typeof(Generator).GetProperty(nameof(Generator.Speed))!)!["siUnit"]!.GetValue<string>(),
            Is.EqualTo("rad/s"));
    }

    [Test]
    public void PressureRatingsAreAbsoluteAndDifferentialPressureRemainsRelative()
    {
        var design = ProviderSemantics.ForProperty(typeof(BopStack).GetProperty(nameof(BopStack.MaxLimitDesignPressure))!)!;
        var differential = ProviderSemantics.ForProperty(typeof(AutoDriller).GetProperty(nameof(AutoDriller.MaxLimitDifferentialPressure))!)!;
        Assert.That(design["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.AbsolutePressureRating));
        Assert.That(design["role"]!.GetValue<string>(), Is.EqualTo(Concepts.MaximumDesignLimit));
        Assert.That(design["reference"]!.GetValue<string>(), Is.EqualTo(Concepts.Vacuum));
        Assert.That(differential["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.PressureDifference));
        Assert.That(differential["reference"], Is.Null);
    }

    [Test]
    public void DrillFloorDepthPublishesWgs84MeanAndOriginFreeUncertainty()
    {
        var property = typeof(FixedPlatformProperties).GetProperty(nameof(FixedPlatformProperties.DrillFloorDepth))!;
        JsonObject bindings = ProviderSemantics.NestedBindings(property)!;
        Assert.That(bindings["/GaussianValue/Mean"]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.DrillFloorDepth));
        Assert.That(bindings["/GaussianValue/Mean"]!["reference"]!.GetValue<string>(), Is.EqualTo(Concepts.Wgs84));
        Assert.That(bindings["/GaussianValue/StandardDeviation"]!["reference"], Is.Null);
        var mcp = McpToolArgumentHelpers.CreateRigSchema()["$defs"]!["FixedPlatformProperties"]!["properties"]!["DrillFloorDepth"]!;
        Assert.That(JsonNode.DeepEquals(mcp[Bindings], bindings), Is.True);
    }

    [Test]
    public void MeasurementCapabilityKeepsDynamicQuantityContext()
    {
        var mcp = McpToolArgumentHelpers.CreateRigSchema()["$defs"]!["EquipmentMeasurementCapability"]!["properties"]!;
        Assert.That(mcp["MinimumValue"]![Semantic]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.MeasurementRangeValue));
        Assert.That(mcp["MinimumValue"]![Semantic]!["physicalQuantity"], Is.Null);
        Assert.That(mcp["AbsoluteAccuracy"]![Semantic]!["concept"]!.GetValue<string>(), Is.EqualTo(Concepts.AbsoluteMeasurementAccuracy));
        Assert.That(mcp["RelativeAccuracy"]![Semantic]!["physicalQuantity"]!["name"]!.GetValue<string>(), Is.EqualTo("ProportionStandard"));
        Assert.That(mcp["UpdateFrequency"]![Semantic]!["physicalQuantity"]!["name"]!.GetValue<string>(), Is.EqualTo("Frequency"));
    }

    [Test]
    public void GeneratedAndMergedOpenApiPreserveReviewedBindings()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rig.sln")))
            directory = directory.Parent;
        string root = directory!.FullName;
        var source = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "ModelSharedOut/json-schemas/RigFullName.json")))!;
        var merged = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "Service/wwwroot/json-schema/RigMergedModel.json")))!;
        JsonNode sourceStandPipe = source["components"]!["schemas"]!["OSDC.Drilling.Rig.Model.StandPipe"]!;
        JsonNode mergedStandPipe = merged["components"]!["schemas"]!["StandPipe"]!;
        foreach (string property in new[] { "PressureMeasurementElevation", "MudHoseHangingPointElevation" })
            Assert.That(JsonNode.DeepEquals(sourceStandPipe["properties"]![property]![Semantic],
                mergedStandPipe["properties"]![property]![Semantic]), Is.True, property);

        JsonNode sourceDepth = source["components"]!["schemas"]!["OSDC.Drilling.Rig.Model.FixedPlatformProperties"]!
            ["properties"]!["DrillFloorDepth"]![Bindings]!;
        JsonNode mergedDepth = merged["components"]!["schemas"]!["FixedPlatformProperties"]!
            ["properties"]!["DrillFloorDepth"]![Bindings]!;
        Assert.That(JsonNode.DeepEquals(sourceDepth, mergedDepth), Is.True);
        Assert.That(merged["paths"]!["/Rig"]!["post"]!["requestBody"]!["content"]!["application/json"]!
            ["schema"]![Semantic]!["referenceProfileVersion"]!.GetValue<string>(), Is.EqualTo("1.1.0"));
    }

    [Test]
    public void RuntimeSwaggerRenderingPreservesSemanticProfileVersions()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Rig.sln")))
            directory = directory.Parent;
        string mergedJson = File.ReadAllText(Path.Combine(directory!.FullName,
            "Service/wwwroot/json-schema/RigMergedModel.json"));

        string renderedJson = SwaggerMiddlewareExtensions.RenderDocument(
            mergedJson, "https://dev.digiwells.no/rig/api");
        JsonNode rendered = JsonNode.Parse(renderedJson)!;

        Assert.Multiple(() =>
        {
            Assert.That(rendered["servers"]![0]!["url"]!.GetValue<string>(),
                Is.EqualTo("https://dev.digiwells.no/rig/api"));
            Assert.That(rendered["paths"]!["/Rig"]!["post"]!["requestBody"]!["content"]!["application/json"]!
                ["schema"]![Semantic]!["referenceProfileVersion"]!.GetValue<string>(), Is.EqualTo("1.1.0"));
            Assert.That(renderedJson, Does.Not.Contain("\"referenceProfileVersion\": \"2000-01-01\""));
        });
    }
}
