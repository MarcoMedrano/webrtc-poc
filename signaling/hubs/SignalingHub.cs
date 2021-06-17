using Kurento.NET;
using Microsoft.AspNetCore.Http.Features;
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
        private readonly RecordingFailover recorder;
        private readonly Settings settings;


        public SignalingHub(ILogger<SignalingHub> logger, IConfiguration configuration, RecordingFailover recorder)
        {
            this.logger = logger;
            this.recorder = recorder;
            this.settings = new Settings();

            configuration.GetSection(nameof(Settings)).Bind(this.settings);
        }

        public override async Task OnConnectedAsync()
        {
            this.logger.LogInformation($"Client {this.Context.ConnectionId} connected");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(System.Exception exception)
        {
            this.logger.LogInformation($"Client {this.Context.ConnectionId} disconnected");
            if (this.Context.Items.TryGetValue("recorder", out object recorderObj))
            {
                var recorder =  (RecordingFailover)recorderObj;
                await recorder.Stop();
            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task Ping()
        {
            this.logger.LogDebug("Ping");
            await this.Clients.Caller.Pong();
        }

        #region SDP & ICE Negotiation
        public async Task AddOffer(string sdpOffer)
        {
            this.logger.LogDebug("Adding remote offer \n" /*+ sdpOffer*/);
            var endpoint = await GetKurentoEndpointAsync();
            var sdpAnswer = await endpoint.ProcessOfferAsync(sdpOffer);

            await Clients.Caller.processAnswer(sdpAnswer);

            await endpoint.GatherCandidatesAsync();
        }

        public async Task AddIceCandidate(string candidateStr /*IceCandidate candidate*/)
        {
            var candidate = JsonConvert.DeserializeObject<IceCandidate>(candidateStr);
            this.logger.LogDebug("Adding remote candidate " + JsonConvert.SerializeObject(candidate));
            var endpoint = await this.GetKurentoEndpointAsync();
            await endpoint.AddIceCandidateAsync(candidate);

        }
        #endregion

        private async Task<WebRtcEndpoint> GetKurentoEndpointAsync()
        {
            WebRtcEndpoint endpoint = null;
            if (this.Context.Items.TryGetValue("kurento_endpoint", out object endpointObj))
            {
                return (WebRtcEndpoint)endpointObj;
            }

            this.logger.LogInformation("CREATING KURENTO ENDPOINT ");

            KurentoClient kurento = null;
            if (this.Context.Items.TryGetValue("kms", out object obj))
            {
                kurento = (KurentoClient)obj;
            }
            else
            {
                Cache.MediaServers.ForEach(kms => this.logger.LogDebug("cache " + kms));
                var kms = LoadBalancer.NextAvailable("mirror");
                kurento = kms.KurentoClient;
                this.Context.Items.Add("kms", kurento);

                var feature = Context.Features.Get<IHttpConnectionFeature>();

                this.logger.LogInformation($"KMS {kms} assigned to {feature.RemoteIpAddress}:{feature.RemotePort}");
            }

            var pipeline = await kurento.CreateAsync(new MediaPipeline());

            endpoint = await kurento.CreateAsync(new WebRtcEndpoint(pipeline));
            await endpoint.SetStunServerAddressAsync(this.settings.Turn.ip);
            await endpoint.SetStunServerPortAsync(this.settings.Turn.port);
            await endpoint.SetTurnUrlAsync($"{this.settings.Turn.username}:{this.settings.Turn.credential}@{this.settings.Turn.ip}:{this.settings.Turn.port}");

            endpoint.OnIceCandidate += arg =>
            {
                this.logger.LogDebug("Kurento ice candidate " + JsonConvert.SerializeObject(arg.candidate));
                Clients.Caller.AddRemoteIceCandidate(JsonConvert.SerializeObject(arg.candidate));
            };

            this.Context.Items.Add("kurento_endpoint", endpoint);

            // await endpoint.ConnectAsync(endpoint);
            await this.recorder.Setup(kurento, endpoint, pipeline);
            this.recorder.Start();

            this.Context.Items.Add("recorder", recorder);

            return endpoint;
        }
    }
}