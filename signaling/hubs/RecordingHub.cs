using Kurento.NET;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace signaling.hubs
{
    public class RecordingHub : DynamicHub
    {
        private readonly KurentoClient kurento;
        private readonly ILogger<RecordingHub> logger;

        public RecordingHub(KurentoClient kurento, ILogger<RecordingHub> logger)
        {
            this.kurento = kurento;
            this.logger = logger;

            this.logger.LogDebug("created hub.");
        }

        public override async Task OnConnectedAsync()
        {
            this.logger.LogDebug("OnConnected " + this.Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public async Task Init() { }

        public async Task Start() { }
        public async Task Stop() { }

        #region ICE Negotiation
        public async Task AddIceCandidate(IceCandidate candidate)
        {
            this.logger.LogDebug("adding candidate", candidate);
            var endpoint = await this.GetKurentoEndpointAsync();
            await endpoint.AddIceCandidateAsync(candidate);

        }
        public async Task AddOffer(string sdpOffer)
        {
            this.logger.LogDebug("adding offer", sdpOffer);
            var endpoint = await GetKurentoEndpointAsync();
            var sdpAnswer = await endpoint.ProcessOfferAsync(sdpOffer);
            Clients.Caller.AddAnswer(sdpAnswer);
            await endpoint.GatherCandidatesAsync();
        }

        private async Task<WebRtcEndpoint> GetKurentoEndpointAsync()
        {
            WebRtcEndpoint endpoint = null;
            if(this.Context.Items.ContainsKey("kurento_endpoint")
            && this.Context.Items.TryGetValue("kurento_endpoint", out object endpointObj))
            {
                return (WebRtcEndpoint)endpointObj;
            }

            var pipeline = await this.kurento.CreateAsync(new MediaPipeline());

            endpoint = await this.kurento.CreateAsync(new WebRtcEndpoint(pipeline));
            endpoint.OnIceCandidate += arg =>
            {
                Clients.Caller.AddIceCandidate(this.Context.ConnectionId, arg.candidate);
            };

            this.Context.Items.Add("kurento_endpoint", endpoint);

            return endpoint;
        }
        #endregion
    }
}