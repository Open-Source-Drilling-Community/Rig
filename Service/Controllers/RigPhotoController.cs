using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Rig.Model;
using OSDC.Drilling.Rig.Service.Managers;

namespace OSDC.Drilling.Rig.Service.Controllers;

[ApiController]
[Route("Rig/{rigId:guid}/Photos")]
public sealed class RigPhotoController : ControllerBase
{
    private readonly RigPhotoManager _photos;
    private readonly RigManager _rigs;
    public RigPhotoController(ILogger<RigManager> logger, SqlConnectionManager connections) { _photos=new(connections); _rigs=RigManager.GetInstance(logger,connections); }

    [HttpGet]
    public ActionResult<IEnumerable<RigPhotoMetadata>> GetAll(Guid rigId) => !_rigs.Contains(rigId) ? NotFound() : Ok(_photos.GetAll(rigId));

    [HttpGet("{photoId:guid}/Content")]
    [Produces("image/jpeg","image/png","image/webp")]
    public ActionResult GetContent(Guid rigId, Guid photoId)
    {
        var value=_photos.Get(rigId,photoId); return value is null ? NotFound() : File(value.Value.Content,value.Value.Metadata.ContentType!,value.Value.Metadata.FileName,enableRangeProcessing:true);
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(RigPhotoManager.MaximumBytes + 65536)]
    public async Task<ActionResult<RigPhotoMetadata>> Create(Guid rigId, IFormFile file, [FromForm] string? title, [FromForm] string? caption, [FromForm] string? alternativeText, [FromForm] int displayOrder=0, [FromForm] bool isPrimary=false, [FromForm] string? source=null, [FromForm] string? attribution=null, [FromForm] string? license=null)
    {
        if (!_rigs.Contains(rigId)) return NotFound();
        await using MemoryStream stream=new(); await file.CopyToAsync(stream);
        RigPhotoMetadata input=new(){Title=title,Caption=caption,AlternativeText=alternativeText,DisplayOrder=displayOrder,IsPrimary=isPrimary,Source=source,Attribution=attribution,License=license};
        RigPhotoMetadata? created=_photos.Create(rigId,file.FileName,file.ContentType,stream.ToArray(),input,out string? error);
        return created is null ? BadRequest(new{error,message="Upload a non-empty JPEG, PNG, or WebP image no larger than 10 MiB."}) : Ok(created);
    }

    [HttpPut("{photoId:guid}")]
    public ActionResult<RigPhotoMetadata> Update(Guid rigId, Guid photoId, [FromQuery] DateTimeOffset? expectedModifiedUtc, [FromBody] RigPhotoMetadata? value)
    {
        if(expectedModifiedUtc is null||value is null) return BadRequest();
        RigPhotoMetadata? updated=_photos.Update(rigId,photoId,expectedModifiedUtc.Value,value,out string? error);
        return error switch { null=>Ok(updated), "not_found"=>NotFound(), "concurrency_conflict"=>Conflict(new{error,message="The photo metadata changed after it was read."}), _=>BadRequest(new{error}) };
    }

    [HttpDelete("{photoId:guid}")]
    public ActionResult Delete(Guid rigId, Guid photoId) => _photos.Delete(rigId,photoId) ? Ok() : NotFound();
}
