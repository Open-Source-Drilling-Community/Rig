using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using OSDC.Drilling.Rig.Service.Managers;
using OSDC.Drilling.Rig.Model;

namespace OSDC.Drilling.Rig.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class RigController : ControllerBase
    {
        private readonly ILogger<RigManager> _logger;
        private readonly RigManager _rigManager;
        private readonly RigFeatureCategoryManager _featureManager;
        private readonly RigPhotoManager _photoManager;
        private readonly IRigExternalReferenceResolver _externalReferenceResolver;

        public RigController(ILogger<RigManager> logger, SqlConnectionManager connectionManager,
            IRigExternalReferenceResolver? externalReferenceResolver = null)
        {
            _logger = logger;
            _rigManager = RigManager.GetInstance(_logger, connectionManager);
            _featureManager = new RigFeatureCategoryManager(_logger, connectionManager);
            _photoManager = new RigPhotoManager(connectionManager);
            _externalReferenceResolver = externalReferenceResolver ?? new UnavailableExternalReferenceResolver();
        }

        /// <summary>
        /// Returns the list of Guid of all Rig present in the microservice database at endpoint Rig/api/Rig
        /// </summary>
        /// <returns>the list of Guid of all Rig present in the microservice database at endpoint Rig/api/Rig</returns>
        [HttpGet(Name = "GetAllRigId")]
        public ActionResult<IEnumerable<Guid>> GetAllRigId()
        {
            UsageStatisticsRig.Instance.IncrementGetAllRigIdPerDay();
            var ids = _rigManager.GetAllRigId();
            if (ids != null)
            {
                return Ok(ids);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns the list of MetaInfo of all Rig present in the microservice database, at endpoint Rig/api/Rig/MetaInfo
        /// </summary>
        /// <returns>the list of MetaInfo of all Rig present in the microservice database, at endpoint Rig/api/Rig/MetaInfo</returns>
        [HttpGet("MetaInfo", Name = "GetAllRigMetaInfo")]
        public ActionResult<IEnumerable<MetaInfo>> GetAllRigMetaInfo()
        {
            UsageStatisticsRig.Instance.IncrementGetAllRigMetaInfoPerDay();
            var vals = _rigManager.GetAllRigMetaInfo();
            if (vals != null)
            {
                return Ok(vals);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns the Rig identified by its Guid from the microservice database, at endpoint Rig/api/Rig/MetaInfo/id
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>the Rig identified by its Guid from the microservice database, at endpoint Rig/api/Rig/MetaInfo/id</returns>
        [HttpGet("{id}", Name = "GetRigById")]
        public ActionResult<RigReadResponse?> GetRigById(Guid id, [FromQuery] bool includePhotos = false)
        {
            UsageStatisticsRig.Instance.IncrementGetRigByIdPerDay();
            if (!id.Equals(Guid.Empty))
            {
                var val = _rigManager.GetRigById(id);
                if (val != null)
                {
                    return Ok(ToReadResponse(val, includePhotos));
                }
                else
                {
                    return NotFound();
                }
            }
            else
            {
                return BadRequest();
            }
        }

        /// <summary>
        /// Returns the list of all RigLight present in the microservice database, at endpoint Rig/api/Rig/LightData
        /// </summary>
        /// <returns>the list of all RigLight present in the microservice database, at endpoint Rig/api/Rig/LightData</returns>
        [HttpGet("LightData", Name = "GetAllRigLight")]
        public ActionResult<IEnumerable<Model.RigLight>> GetAllRigLight()
        {
            UsageStatisticsRig.Instance.IncrementGetAllRigLightPerDay();
            var vals = _rigManager.GetAllRigLight();
            if (vals != null)
            {
                return Ok(vals);
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Returns the list of all Rig present in the microservice database, at endpoint Rig/api/Rig/HeavyData
        /// </summary>
        /// <returns>the list of all Rig present in the microservice database, at endpoint Rig/api/Rig/HeavyData</returns>
        [HttpGet("HeavyData", Name = "GetAllRig")]
        public ActionResult<IEnumerable<RigReadResponse?>> GetAllRig([FromQuery] bool includePhotos = false)
        {
            UsageStatisticsRig.Instance.IncrementGetAllRigPerDay();
            var vals = _rigManager.GetAllRig();
            if (vals != null)
            {
                return Ok(vals.Select(value => value is null ? null : ToReadResponse(value, includePhotos)).ToList());
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Performs calculation on the given Rig and adds it to the microservice database, at the endpoint Rig/api/Rig
        /// </summary>
        /// <param name="rig"></param>
        /// <returns>true if the given Rig has been added successfully to the microservice database, at the endpoint Rig/api/Rig</returns>
        [HttpPost(Name = "PostRig")]
        public ActionResult PostRig([FromBody] Model.Rig? data)
        {
            UsageStatisticsRig.Instance.IncrementPostRigPerDay();
            // Check if rig exists in the database through ID
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID != Guid.Empty)
            {
                List<string> featureErrors = _featureManager.ValidateAssignments(data.FeatureAssignments);
                featureErrors.AddRange(RigDefinitionValidator.Validate(data));
                if (featureErrors.Count > 0) return BadRequest(new { error = "invalid_rig_definition", errors = featureErrors });
                var existingData = _rigManager.GetRigById(data.MetaInfo.ID);
                if (existingData == null)
                {   
                    //  If rig was not found, call AddRig, where the rig.Calculate()
                    // method is called. 
                    if (_rigManager.AddRig(data))
                    {
                        return Ok(); // status=OK is used rather than status=Created because NSwag auto-generated controllers use 200 (OK) rather than 201 (Created) as return codes
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    _logger.LogWarning("The given Rig already exists and will not be added");
                    return StatusCode(StatusCodes.Status409Conflict);
                }
            }
            else
            {
                _logger.LogWarning("The given Rig is null, badly formed, or its ID is empty");
                return BadRequest();
            }
        }

        /// <summary>
        /// Performs calculation on the given Rig and updates it in the microservice database, at the endpoint Rig/api/Rig/id
        /// </summary>
        /// <param name="rig"></param>
        /// <returns>true if the given Rig has been updated successfully to the microservice database, at the endpoint Rig/api/Rig/id</returns>
        [HttpPut("{id}", Name = "PutRigById")]
        [ProducesResponseType<Model.Rig>(StatusCodes.Status200OK)]
        [ProducesResponseType<RigMutationErrorEnvelope>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<RigMutationErrorEnvelope>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<RigMutationErrorEnvelope>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<RigMutationErrorEnvelope>(StatusCodes.Status500InternalServerError)]
        public ActionResult<Model.Rig> PutRigById(Guid id,
            [FromQuery, BindRequired] DateTimeOffset expectedModifiedUtc, [FromBody] Model.Rig? data)
        {
            UsageStatisticsRig.Instance.IncrementPutRigByIdPerDay();
            // Check if Rig is in the data base
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID.Equals(id) && expectedModifiedUtc != default)
            {
                List<string> featureErrors = _featureManager.ValidateAssignments(data.FeatureAssignments);
                featureErrors.AddRange(RigDefinitionValidator.Validate(data));
                if (featureErrors.Count > 0) return BadRequest(new RigMutationErrorEnvelope
                {
                    Error = "invalid_rig_definition", Message = "The rig definition is invalid.",
                    Errors = featureErrors.Select(message => new RigMutationError
                    { Property = "rig", Code = "invalid_value", Message = message }).ToList()
                });
                RigUpdateOutcome outcome = _rigManager.UpdateRigById(id, expectedModifiedUtc, data);
                if (outcome.IsSuccess) return Ok(outcome.Rig);
                return outcome.FailureKind switch
                {
                    RigUpdateFailureKind.InvalidRequest => BadRequest(outcome.Error),
                    RigUpdateFailureKind.NotFound => NotFound(outcome.Error),
                    RigUpdateFailureKind.Conflict => Conflict(outcome.Error),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, outcome.Error)
                };
            }
            else
            {
                _logger.LogWarning("The given Rig is null, badly formed, or its does not match the ID to update");
                return BadRequest(new RigMutationErrorEnvelope
                {
                    Error = "invalid_request",
                    Message = "The route UUID, rig.MetaInfo.ID, and expectedModifiedUtc are required and must agree.",
                    Errors = [new RigMutationError { Property = "expectedModifiedUtc", Code = "required", Message = "Supply the LastModificationDate returned by the latest rig read." }]
                });
            }
        }

        /// <summary>
        /// Deletes the Rig of given ID from the microservice database, at the endpoint Rig/api/Rig/id
        /// </summary>
        /// <param name="guid"></param>
        /// <returns>true if the Rig was deleted from the microservice database, at the endpoint Rig/api/Rig/id</returns>
        [HttpDelete("{id}", Name = "DeleteRigById")]
        public ActionResult DeleteRigById(Guid id)
        {
            UsageStatisticsRig.Instance.IncrementDeleteRigByIdPerDay();
            if (_rigManager.GetRigById(id) != null)
            {
                if (_rigManager.DeleteRigById(id))
                {
                    _photoManager.DeleteAll(id);
                    return Ok();
                }
                else
                {
                    return StatusCode(StatusCodes.Status500InternalServerError);
                }
            }
            else
            {
                _logger.LogWarning("The Rig of given ID does not exist");
                return NotFound();
            }
        }

        [HttpPost("BatchExport", Name = "BatchExportRigs")]
        [ProducesResponseType<RigBatchExportDocument>(StatusCodes.Status200OK)]
        [ProducesResponseType<RigBatchErrorEnvelope>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<RigBatchErrorEnvelope>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<RigBatchErrorEnvelope>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<RigBatchErrorEnvelope>(StatusCodes.Status502BadGateway)]
        public async Task<ActionResult<RigBatchExportDocument>> BatchExportRigs(
            [FromBody] RigBatchExportRequest? request, CancellationToken cancellationToken)
        {
            UsageStatisticsRig.Instance.IncrementBatchExportRigsPerDay();
            RigBatchExportOutcome outcome = _rigManager.ExportBatch(request, _featureManager.GetAll(), _photoManager.GetForBatch);
            if (!outcome.IsSuccess)
                return outcome.FailureKind switch
                {
                    RigBatchExportFailureKind.InvalidRequest => BadRequest(outcome.Error),
                    RigBatchExportFailureKind.RigNotFound => NotFound(outcome.Error),
                    _ => StatusCode(StatusCodes.Status500InternalServerError, outcome.Error)
                };
            try
            {
                List<RigBatchError> errors = await _externalReferenceResolver.PopulateExportManifestAsync(outcome.Document!, cancellationToken);
                return errors.Count == 0 ? Ok(outcome.Document) : Conflict(new RigBatchErrorEnvelope
                { Error = "external_reference_invalid", Message = "One or more Cluster references could not be represented in the portable backup.", Errors = errors });
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Unable to resolve Cluster names during rig export");
                return StatusCode(StatusCodes.Status502BadGateway, ExternalServiceError(ex.Message));
            }
        }

        [HttpPost("BatchRestore", Name = "BatchRestoreRigs")]
        [ProducesResponseType<RigBatchRestoreResponse>(StatusCodes.Status200OK)]
        [ProducesResponseType<RigBatchErrorEnvelope>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<RigBatchErrorEnvelope>(StatusCodes.Status409Conflict)]
        [ProducesResponseType<RigBatchErrorEnvelope>(StatusCodes.Status502BadGateway)]
        [RequestSizeLimit(512L * 1024L * 1024L)]
        public async Task<ActionResult<RigBatchRestoreResponse>> BatchRestoreRigs(
            [FromBody] RigBatchRestoreRequest? request, CancellationToken cancellationToken)
        {
            UsageStatisticsRig.Instance.IncrementBatchRestoreRigsPerDay();
            List<RigBatchError> requestErrors = RigBatchRestorer.ValidateRequest(request);
            if (requestErrors.Count != 0) return BadRequest(new RigBatchErrorEnvelope
            { Error = "invalid_batch_restore_request", Message = "The rig batch-restore request is invalid. No changes were made.", Errors = requestErrors });
            RigExternalReferenceResolutionOutcome external;
            try { external = await _externalReferenceResolver.ResolveRestoreManifestAsync(request!.Document!, cancellationToken); }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Unable to resolve destination Cluster references during rig restore");
                return StatusCode(StatusCodes.Status502BadGateway, ExternalServiceError(ex.Message));
            }
            if (!external.IsSuccess) return Conflict(new RigBatchErrorEnvelope
            { Error = "external_reference_mapping_failed", Message = "Cluster references could not be resolved uniquely. No changes were made.", Errors = external.Errors });
            _featureManager.GetAll(); // Seed immutable built-ins before transactional catalog mapping starts.
            RigBatchRestoreOutcome outcome = _rigManager.RestoreBatch(request, external.Mappings);
            if (outcome.IsSuccess) return Ok(outcome.Response);
            return outcome.FailureKind switch
            {
                RigBatchRestoreFailureKind.InvalidRequest => BadRequest(outcome.Error),
                RigBatchRestoreFailureKind.Conflict => Conflict(outcome.Error),
                _ => StatusCode(StatusCodes.Status500InternalServerError, outcome.Error)
            };
        }

        private static RigBatchErrorEnvelope ExternalServiceError(string message) => new()
        {
            Error = "external_reference_service_unavailable",
            Message = "Cluster reference validation could not be completed. No changes were made.",
            Errors = [new RigBatchError { Property = "ExternalReferences", Code = "dependency_unavailable", Message = message }]
        };

        private sealed class UnavailableExternalReferenceResolver : IRigExternalReferenceResolver
        {
            public Task<List<RigBatchError>> PopulateExportManifestAsync(RigBatchExportDocument document, CancellationToken cancellationToken) =>
                throw new HttpRequestException("Cluster reference resolution is not configured for this controller instance.");
            public Task<RigExternalReferenceResolutionOutcome> ResolveRestoreManifestAsync(RigBatchExportDocument document, CancellationToken cancellationToken) =>
                throw new HttpRequestException("Cluster reference resolution is not configured for this controller instance.");
        }

        private RigReadResponse ToReadResponse(Model.Rig value, bool includePhotos)
        {
            RigReadResponse response = System.Text.Json.JsonSerializer.Deserialize<RigReadResponse>(
                System.Text.Json.JsonSerializer.Serialize(value, JsonSettings.Options), JsonSettings.Options)!;
            if (includePhotos) response.Photos = _photoManager.GetAll(value.MetaInfo!.ID);
            return response;
        }
    }
}
