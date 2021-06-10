using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace lb_agent.Controllers
{

    [ApiController]
    [Route("[controller]")]
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
            this.Logger.LogDebug("Printing in memory Agent");
            return "Alive";
        }

        [HttpGet]
        [Route("stats/memory")]

        public Memory GetStats()
        {
            this.Logger.LogDebug("Printing stats");
            return SystemStats.Memory;
        }

        /// <summary>
        /// Receives a json object in the body contentType:application/json
        /// </summary>
        /// <param name="value" >
        /// A json object.
        /// <example>"{'a':'event'}"</example>
        /// </param>
        /// <response code="200">OK</response>
        [HttpPost]
        [Route("stats/memory/used")]
        public void PostJson([FromBody]int value)
        {
            this.Logger.LogDebug("Value received " + value);
            SystemStats.Memory.Used = value;
        }
    }
}
