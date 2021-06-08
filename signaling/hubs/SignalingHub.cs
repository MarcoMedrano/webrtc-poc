using Kurento.NET;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace signaling.hubs
{
    public class SignalingHub : DynamicHub
    {
        private readonly ILogger<SignalingHub> logger;
        private readonly Settings settings;


        public SignalingHub(ILogger<SignalingHub> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.settings = new Settings();

            configuration.GetSection(nameof(Settings)).Bind(this.settings);
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
            var endpoint = await this.GetKurentoEndpointAsync();
            await endpoint.AddIceCandidateAsync(candidate);

        }
        public async Task AddOffer(string sdpOffer)
        {
            this.logger.LogDebug("Adding remote offer \n" + sdpOffer);
            var endpoint = await GetKurentoEndpointAsync();
            var sdpAnswer = await endpoint.ProcessOfferAsync(sdpOffer);

            await Clients.Caller.processAnswer(sdpAnswer);

            await endpoint.GatherCandidatesAsync();
        }

        #endregion

        private async Task<WebRtcEndpoint> GetKurentoEndpointAsync()
        {
            WebRtcEndpoint endpoint = null;
            if(this.Context.Items.TryGetValue("kurento_endpoint", out object endpointObj))
            {
                return (WebRtcEndpoint)endpointObj;
            }

            this.logger.LogDebug("CREATING KURENTO ENDPOINT ");

            KurentoClient kurento = null;
            if(this.Context.Items.TryGetValue("kms", out object obj))
            {
                kurento = (KurentoClient)obj;
            }
            else
            {
                kurento = LoadBalancer.NextAvailable().KurentoClient;
                this.Context.Items.Add("kms", kurento);

            }

            var pipeline = await kurento.CreateAsync(new MediaPipeline());

            endpoint = await kurento.CreateAsync(new WebRtcEndpoint(pipeline));
            await endpoint.SetStunServerAddressAsync(this.settings.Turn.ip);
            await endpoint.SetStunServerPortAsync(this.settings.Turn.port);
            await endpoint.SetTurnUrlAsync($"{this.settings.Turn.username}:{this.settings.Turn.credential}@{this.settings.Turn.ip}:{this.settings.Turn.port}");

            endpoint.OnIceCandidate += arg =>
            {
                this.logger.LogInformation("Kurento ice candidate " + JsonConvert.SerializeObject(arg.candidate));
                Clients.Caller.AddRemoteIceCandidate(JsonConvert.SerializeObject(arg.candidate));
            };

            this.Context.Items.Add("kurento_endpoint", endpoint);

            await endpoint.ConnectAsync(endpoint);
            // await this.CreateRecorderEndpointAsync(endpoint, pipeline);

            return endpoint;
        }
    }
}