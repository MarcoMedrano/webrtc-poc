using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace lb_agent.Controllers
{

    [Route("api/[controller]")]
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
        [Route("stats")]

        public string GetStats()
        {
            this.Logger.LogDebug("Printing stats");
            return JsonConvert.SerializeObject(SystemStats.Memory);
        }

        /// <summary>
        /// Receives a json objct in the body contentType:application/json
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
            this.Logger.LogDebug("Value recived " + value);
            SystemStats.Memory.Used = value;
        }

        /// <summary>
        /// Receives any contentType in the body
        /// </summary>
        /// <example>Another event</example>
        [HttpPost]
        [Route("raw")]
        public async Task PostRaw()
        {
            this.Logger.LogDebug("RAW received");
            using (StreamReader reader = new StreamReader(Request.Body, Encoding.UTF8))
            {
                string value = await reader.ReadToEndAsync();
                return;
            }
        }

    }
}
