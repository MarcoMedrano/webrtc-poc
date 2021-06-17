using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace lb_agent.Controllers
{

    [ApiController]
    [Route("agent")]
    public class AgentController : Controller
    {
        private ILogger Logger { get; set; }

        public AgentController(ILogger<AgentController> logger)
        {
            this.Logger = logger;
        }

        /// <summary>
        /// Gets the Agent status
        /// </summary>
        [HttpGet]
        public string Get()
        {
            return "Alive";
        }

        /// <summary> 
        /// Gets the computer stats, memory, cpu, disk, network
        /// </summary>
        [HttpGet]
        [Route("memory")]
        public Memory GetStats()
        {
            this.Logger.LogDebug("Printing memory status");
            return SystemStats.Memory;
        }

        /// <summary>
        /// Receives the amount of memory available this agent should report
        /// </summary>
        /// <param name="value" >
        /// A integer.
        /// <example>512</example>
        /// </param>
        /// <response code="200">OK</response>
        [HttpPost]
        [Route("memory/available")]
        public void PostMemoryAvailable([FromBody] int value)
        {
            this.Logger.LogDebug("Memory available received " + value);
            SystemStats.Memory.Available = value;
        }

        /// <summary>
        /// Receives wether this agent host should be in maintenance mode or not
        /// </summary>
        [HttpPost]
        [Route("maintenance_mode")]
        public void PostMaintenanceMode([FromBody] bool value)
        {
            this.Logger.LogDebug("Maintenance mode received " + value);
            SystemStats.MaintenanceMode = value;
        }
    }
}
