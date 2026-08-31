using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;
using OSDC.Drilling.Rig.Service.Controllers;
using OSDC.Drilling.Rig.Service.Mcp;
using OSDC.Drilling.Rig.Service.Mcp.Tools;

namespace ServiceTest;

[TestFixture]
public sealed class McpToolRegistrationTests
{
    private static readonly IReadOnlyDictionary<string, string> EndpointToolMap = new Dictionary<string, string>
    {
        ["GetAllRigId"] = "rig_get_all_ids",
        ["GetAllRigMetaInfo"] = "rig_get_all_meta_info",
        ["GetRigById"] = "rig_get_by_id",
        ["GetAllRigLight"] = "rig_get_all_light",
        ["GetAllRig"] = "rig_get_all",
        ["PostRig"] = "rig_create",
        ["PutRigById"] = "rig_update_by_id",
        ["DeleteRigById"] = "rig_delete_by_id",
        ["BatchExportRigs"] = "rig_batch_export",
        ["BatchRestoreRigs"] = "rig_batch_restore",
        ["GetAllIds"] = "rig_feature_category_get_all_ids",
        ["GetAllMetaInfo"] = "rig_feature_category_get_all_meta_info",
        ["GetAll"] = "rig_feature_category_get_all",
        ["GetById"] = "rig_feature_category_get_by_id",
        ["Create"] = "rig_feature_category_create",
        ["Update"] = "rig_feature_category_update_by_id",
        ["Delete"] = "rig_feature_category_delete_by_id"
    };

    private ServiceProvider _provider = null!;
    private IReadOnlyDictionary<string, IMcpTool> _tools = null!;

    [SetUp]
    public void SetUp()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLegacyMcpTool<PingMcpTool>();
        services.AddRigRestMcpTools();
        _provider = services.BuildServiceProvider();
        _tools = _provider.GetServices<IMcpTool>().ToDictionary(tool => tool.Name);
    }

    [TearDown]
    public void TearDown() => _provider.Dispose();

    [Test]
    public void Every_non_statistics_controller_endpoint_has_a_registered_tool()
    {
        var endpointMethods = new[] { typeof(RigController), typeof(RigFeatureCategoryController) }
            .SelectMany(type => type.GetMethods())
            .Where(method => method.GetCustomAttributes(typeof(HttpMethodAttribute), true).Length > 0)
            .Select(method => method.Name);
        Assert.That(endpointMethods, Is.EquivalentTo(EndpointToolMap.Keys));
        Assert.That(_tools.Keys, Is.EquivalentTo(EndpointToolMap.Values.Append("ping")));
    }

    [Test]
    public void Rig_controller_has_one_unambiguous_public_constructor() =>
        Assert.That(typeof(RigController).GetConstructors(), Has.Length.EqualTo(1));

    [Test]
    public void Usage_statistics_are_not_exposed() => Assert.That(_tools.Keys, Has.None.Contains("statistics"));

    [Test]
    public void Domain_tools_have_detailed_descriptions_and_explicit_object_schemas()
    {
        IMcpTool[] domainTools = _tools.Values.Where(tool => tool.Name != "ping").ToArray();

        Assert.That(domainTools.All(tool => tool.Description.Length >= 150), Is.True);
        Assert.That(domainTools.All(tool => tool.InputSchema is JsonObject), Is.True);
        Assert.That(domainTools.All(tool => tool.InputSchema?["type"]?.GetValue<string>() == "object"), Is.True);
    }

    [Test]
    public void Rig_write_schema_covers_platform_relationship_and_nested_equipment_contract()
    {
        JsonObject schema = (JsonObject)_tools["rig_create"].InputSchema!;
        JsonObject definitions = (JsonObject)schema["$defs"]!;
        string json = schema.ToJsonString();

        Assert.That(definitions.Count, Is.GreaterThan(40));
        Assert.That(json, Does.Contain("IsFixedPlatform"));
        Assert.That(json, Does.Contain("ClusterID"));
        Assert.That(json, Does.Contain("external reference to the Cluster microservice"));
        Assert.That(json, Does.Contain("MainRigMast"));
        Assert.That(json, Does.Contain("MudPumpList"));
        Assert.That(json, Does.Contain("LinerConfigurations"));
        Assert.That(json, Does.Contain("LinerInnerDiameter"));
        Assert.That(json, Does.Contain("MaximumVolumetricFlowRate"));
        Assert.That(json, Does.Contain("MaximumDischargePressure"));
        Assert.That(json, Does.Not.Contain("LinerId"));
        Assert.That(json, Does.Contain("BopStack"));
        Assert.That(json, Does.Contain("TopDriveControllerType"));
        Assert.That(json, Does.Contain("StiffPIController"));
        Assert.That(json, Does.Contain("MeasurementCapabilities"));
        Assert.That(json, Does.Contain("MeasurementCode"));
        Assert.That(json, Does.Contain("PhysicalQuantity"));
        Assert.That(json, Does.Contain("never contain live measurement values"));
        Assert.That(json, Does.Not.Contain("EnginePower"));
        Assert.That(json, Does.Not.Contain("PressureBeforeChoke"));
        Assert.That(json, Does.Not.Contain("MudPumpStrokeRate"));
    }

    [Test]
    public void Rig_write_schema_documents_si_units_and_update_identity_rule()
    {
        string createSchema = _tools["rig_create"].InputSchema!.ToJsonString();
        string updateSchema = _tools["rig_update_by_id"].InputSchema!.ToJsonString();

        Assert.That(createSchema, Does.Contain("Drill-floor elevation in metres"));
        Assert.That(createSchema, Does.Contain("pascal (Pa)"));
        Assert.That(createSchema, Does.Contain("newton metre"));
        Assert.That(createSchema, Does.Contain("radian per second"));
        Assert.That(createSchema, Does.Contain("do not send a display-unit value"));
        Assert.That(updateSchema, Does.Contain("must exactly equal rig.MetaInfo.ID"));
    }

    [Test]
    public void Protocol_tool_names_are_valid_and_unique()
    {
        var names = _provider.GetServices<McpServerTool>().Select(tool => tool.ProtocolTool.Name).ToArray();
        Assert.That(names, Has.Length.EqualTo(_tools.Count));
        Assert.That(names, Is.Unique);
        Assert.That(names.All(name => !name.Contains('.')), Is.True);
    }

    [TestCase("rig_get_by_id")]
    public async Task Get_by_id_tools_require_an_id(string toolName)
    {
        var response = await _tools[toolName].InvokeAsync(new JsonObject(), CancellationToken.None) as JsonObject;
        Assert.That(response?["status"]?.GetValue<int>(), Is.EqualTo(400));
        Assert.That(response?["error"]?.GetValue<string>(), Does.Contain("id"));
    }

    [Test]
    public async Task Create_tool_requires_a_request_body()
    {
        var response = await _tools["rig_create"].InvokeAsync(new JsonObject(), CancellationToken.None) as JsonObject;
        Assert.That(response?["status"]?.GetValue<int>(), Is.EqualTo(400));
    }

    [Test]
    public void Rig_reads_make_photo_metadata_explicit_and_opt_in()
    {
        foreach (string toolName in new[] { "rig_get_by_id", "rig_get_all" })
        {
            string schema = _tools[toolName].InputSchema!.ToJsonString();
            Assert.That(schema, Does.Contain("includePhotos"));
            Assert.That(schema, Does.Contain("Image bytes are never returned by MCP"));
            Assert.That(_tools[toolName].Description, Does.Contain("image bytes are never"));
        }
    }
}
