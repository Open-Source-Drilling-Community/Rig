using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Rig.Model;
using OSDC.Drilling.Rig.Service.Managers;

namespace OSDC.Drilling.Rig.Service.Controllers;

[Produces("application/json")]
[Route("[controller]")]
[ApiController]
public sealed class RigFeatureCategoryController : ControllerBase
{
    private readonly RigFeatureCategoryManager _manager;

    public RigFeatureCategoryController(ILogger<RigFeatureCategoryManager> logger, SqlConnectionManager connections)
    {
        _manager = new RigFeatureCategoryManager(logger, connections);
    }

    [HttpGet(Name = "GetAllRigFeatureCategoryId")]
    public ActionResult<IEnumerable<Guid>> GetAllIds() => Ok(_manager.GetAllIds());

    [HttpGet("MetaInfo", Name = "GetAllRigFeatureCategoryMetaInfo")]
    public ActionResult<IEnumerable<MetaInfo?>> GetAllMetaInfo() => Ok(_manager.GetAllMetaInfo());

    [HttpGet("HeavyData", Name = "GetAllRigFeatureCategory")]
    public ActionResult<IEnumerable<RigFeatureCategory>> GetAll() => Ok(_manager.GetAll());

    [HttpGet("{id}", Name = "GetRigFeatureCategoryById")]
    public ActionResult<RigFeatureCategory> GetById(Guid id)
    {
        if (id == Guid.Empty) return BadRequest();
        RigFeatureCategory? value = _manager.GetById(id);
        return value is null ? NotFound() : Ok(value);
    }

    [HttpPost(Name = "PostRigFeatureCategory")]
    public ActionResult<RigFeatureCategory> Create([FromBody] RigFeatureCategory? value)
    {
        if (value is null) return BadRequest();
        RigFeatureCategory? created = _manager.CreateCustom(value, out string? error);
        if (created is null) return Conflict(new { error = "invalid_feature_category", message = error });
        return Ok(created);
    }

    [HttpPut("{id}", Name = "PutRigFeatureCategoryById")]
    public ActionResult<RigFeatureCategory> Update(Guid id, [FromQuery] DateTimeOffset? expectedModifiedUtc, [FromBody] RigFeatureCategory? value)
    {
        if (id == Guid.Empty || expectedModifiedUtc is null || value is null) return BadRequest();
        RigFeatureCategory? updated = _manager.UpdateCustom(id, expectedModifiedUtc.Value, value, out string? error);
        return error switch
        {
            null => Ok(updated),
            "not_found" => NotFound(),
            "concurrency_conflict" => Conflict(new { error, message = "The category changed after it was read." }),
            "built_in_immutable" => Conflict(new { error, message = "Built-in rig feature categories are immutable." }),
            _ => Conflict(new { error = "invalid_feature_category", message = error })
        };
    }

    [HttpDelete("{id}", Name = "DeleteRigFeatureCategoryById")]
    public ActionResult Delete(Guid id)
    {
        if (id == Guid.Empty) return BadRequest();
        if (_manager.DeleteCustom(id, out string? error)) return Ok();
        return error switch
        {
            "not_found" => NotFound(),
            "built_in_immutable" => Conflict(new { error, message = "Built-in rig feature categories are immutable." }),
            "category_in_use" => Conflict(new { error, message = "The category is assigned to at least one rig." }),
            _ => StatusCode(StatusCodes.Status500InternalServerError)
        };
    }
}
