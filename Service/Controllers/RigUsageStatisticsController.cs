using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using OSDC.Drilling.Rig.Model;

namespace OSDC.Drilling.Rig.Service.Controllers
{
    [Produces("application/json")]
    [Route("[controller]")]
    [ApiController]
    public class RigUsageStatisticsController : ControllerBase
    {
        private readonly ILogger _logger;

        public RigUsageStatisticsController(ILogger<RigUsageStatisticsController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Returns the usage statistics present in the microservice database at endpoint Rig/api/RigUsageStatistics
        /// </summary>
        /// <returns></returns>
        [HttpGet(Name = "GetRigUsageStatistics")]
        public ActionResult<UsageStatisticsRig> GetRigUsageStatistics()
        {
            if (UsageStatisticsRig.Instance != null)
            {
                return Ok(UsageStatisticsRig.Instance);
            }
            else
            {
                return NotFound();
            }
        }
    }
}
