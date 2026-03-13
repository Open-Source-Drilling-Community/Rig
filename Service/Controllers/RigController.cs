using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSDC.DotnetLibraries.General.DataManagement;
using NORCE.Drilling.Rig.Service.Managers;
using NORCE.Drilling.Rig.Model;

namespace NORCE.Drilling.Rig.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class RigController : ControllerBase
    {
        private readonly ILogger<RigManager> _logger;
        private readonly RigManager _rigManager;

        public RigController(ILogger<RigManager> logger, SqlConnectionManager connectionManager)
        {
            _logger = logger;
            _rigManager = RigManager.GetInstance(_logger, connectionManager);
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
        public ActionResult<Model.Rig?> GetRigById(Guid id)
        {
            UsageStatisticsRig.Instance.IncrementGetRigByIdPerDay();
            if (!id.Equals(Guid.Empty))
            {
                var val = _rigManager.GetRigById(id);
                if (val != null)
                {
                    return Ok(val);
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
        public ActionResult<IEnumerable<Model.Rig?>> GetAllRig()
        {
            UsageStatisticsRig.Instance.IncrementGetAllRigPerDay();
            var vals = _rigManager.GetAllRig();
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
        public ActionResult PutRigById(Guid id, [FromBody] Model.Rig? data)
        {
            UsageStatisticsRig.Instance.IncrementPutRigByIdPerDay();
            // Check if Rig is in the data base
            if (data != null && data.MetaInfo != null && data.MetaInfo.ID.Equals(id))
            {
                var existingData = _rigManager.GetRigById(id);
                if (existingData != null)
                {
                    if (_rigManager.UpdateRigById(id, data))
                    {
                        return Ok();
                    }
                    else
                    {
                        return StatusCode(StatusCodes.Status500InternalServerError);
                    }
                }
                else
                {
                    _logger.LogWarning("The given Rig has not been found in the database");
                    return NotFound();
                }
            }
            else
            {
                _logger.LogWarning("The given Rig is null, badly formed, or its does not match the ID to update");
                return BadRequest();
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
    }
}
