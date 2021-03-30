using Kurento.NET;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        }

        public override async Task OnConnectedAsync()
        {
            this.logger.LogDebug("Client connected with ID " + this.Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public async Task Init() { }

        public async Task Start() { }
        public async Task Stop() { }

        public async Task Ping() {
            this.logger.LogDebug("Ping");
            await this.Clients.Caller.Pong();
        }

        #region ICE Negotiation
        public async Task AddIceCandidate(string candidateStr /*IceCandidate candidate*/)
        {
            var candidate = JsonConvert.DeserializeObject<IceCandidate>(candidateStr);
            this.logger.LogDebug("Adding candidate " + JsonConvert.SerializeObject(candidate));
            var endpoint = await this.GetKurentoEndpointAsync();
            await endpoint.AddIceCandidateAsync(candidate);

        }
        public async Task AddSdp(string sdpOffer)
        {
            this.logger.LogDebug("Adding offer " + sdpOffer);
            var endpoint = await GetKurentoEndpointAsync();
            var sdpAnswer = await endpoint.ProcessOfferAsync(sdpOffer);

            await Clients.Caller.AddRemoteSdp(sdpAnswer);

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

            this.logger.LogDebug("CREATING KURENTO ENDPOINT ");

            var pipeline = await this.kurento.CreateAsync(new MediaPipeline());

            endpoint = await this.kurento.CreateAsync(new WebRtcEndpoint(pipeline));
            endpoint.OnIceCandidate += arg =>
            {
                this.logger.LogInformation("Kurento ice candidate " + JsonConvert.SerializeObject(arg.candidate));
                Clients.Caller.AddRemoteIceCandidate(JsonConvert.SerializeObject(arg.candidate));
            };

            this.Context.Items.Add("kurento_endpoint", endpoint);

            await endpoint.ConnectAsync(endpoint);

            return endpoint;
        }
        #endregion
    }
}