using Kurento.NET;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace signaling.hubs
{
    public class LiveMonitoringHub : DynamicHub
    {
        private readonly ILogger<LiveMonitoringHub> logger;

        public LiveMonitoringHub(ILogger<LiveMonitoringHub> logger)
        {
            this.logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            this.logger.LogDebug("Client connected with ID " + this.Context.ConnectionId);
            await base.OnConnectedAsync();
        }


        public async Task Ping()
        {
            this.logger.LogDebug("Ping");
            await this.Clients.Caller.Pong();
        }

        #region ICE Negotiation
        public async Task AddIceCandidate(string candidateStr /*IceCandidate candidate*/)
        {
            var candidate = JsonConvert.DeserializeObject<IceCandidate>(candidateStr);
            this.logger.LogDebug("Adding remote candidate " + JsonConvert.SerializeObject(candidate));
            this.Clients.Others.AddRemoteIceCandidate(JsonConvert.SerializeObject(candidate));

        }
        public async Task AddOffer(string sdp)
        {
            this.logger.LogDebug("Adding offer \n" + sdp);
            await this.Clients.Others.ProcessOffer(sdp);
        }

        public async Task AddAnswer(string sdp)
        {
            this.logger.LogDebug("Adding answer \n" + sdp);
            await this.Clients.Others.ProcessAnswer(sdp);
        }

        #endregion
    }
}