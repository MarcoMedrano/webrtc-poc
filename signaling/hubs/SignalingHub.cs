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

            this.logger.LogDebug("CREATING KURENTO ENDPOINT ");

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
                this.logger.LogInformation("Kurento ice candidate " + JsonConvert.SerializeObject(arg.candidate));
                Clients.Caller.AddRemoteIceCandidate(JsonConvert.SerializeObject(arg.candidate));
            };

            this.Context.Items.Add("kurento_endpoint", endpoint);

            // await endpoint.ConnectAsync(endpoint);
            await this.CreateRecorderEndpointAsync(kurento, endpoint, pipeline);

            return endpoint;
        }

        private async Task<RecorderEndpoint> CreateRecorderEndpointAsync(KurentoClient kurentoMirror, WebRtcEndpoint receiverEndpoint, MediaPipeline pipeline)
        {
            var mirrorEndpoint = await kurentoMirror.CreateAsync(new WebRtcEndpoint(pipeline));
            await receiverEndpoint.ConnectAsync(mirrorEndpoint);


            
            var kms = LoadBalancer.NextAvailable("recorder");
            var kurento = kms.KurentoClient;

            var pipelineRecorder = await kurento.CreateAsync(new MediaPipeline());
            var preRecorderEndpoint = await kurento.CreateAsync(new WebRtcEndpoint(pipelineRecorder));
            // await endpoint.SetStunServerAddressAsync(this.settings.Turn.ip);
            // await endpoint.SetStunServerPortAsync(this.settings.Turn.port);
            // await endpoint.SetTurnUrlAsync($"{this.settings.Turn.username}:{this.settings.Turn.credential}@{this.settings.Turn.ip}:{this.settings.Turn.port}");

            preRecorderEndpoint.OnIceCandidate += async arg =>
            {
                this.logger.LogInformation("Kurento Recorder ice candidate " + JsonConvert.SerializeObject(arg.candidate));
                await mirrorEndpoint.AddIceCandidateAsync(arg.candidate);
            };

            mirrorEndpoint.OnIceCandidate += async arg =>
            {
                this.logger.LogInformation("Kurento Mirror ice candidate " + JsonConvert.SerializeObject(arg.candidate));
                await preRecorderEndpoint.AddIceCandidateAsync(arg.candidate);
            };

            var offer = await mirrorEndpoint.GenerateOfferAsync();
            var answer = await preRecorderEndpoint.ProcessOfferAsync(offer);
            await mirrorEndpoint.ProcessAnswerAsync(answer); //maybe not needed OR preRecorderEndpoint.processAnswerAsync needed as well.

            await mirrorEndpoint.GatherCandidatesAsync();
            await preRecorderEndpoint.GatherCandidatesAsync();

            RecorderEndpoint recorder = await kurento.CreateAsync(new RecorderEndpoint(pipelineRecorder, $"file:///tmp/1-{kms.Ip}.webm", MediaProfileSpecType.WEBM_VIDEO_ONLY));
            recorder.Recording += (e) => this.logger.LogInformation("Recording"); 
            
            if(this.Context.Items.ContainsKey("recorder_endpoint")) {
                this.Context.Items.Remove("recorder_endpoint");
            }

            this.Context.Items.Add("recorder_endpoint", recorder);
            await preRecorderEndpoint.ConnectAsync(recorder, MediaType.VIDEO, "default", "default");

            await recorder.RecordAsync();

            return recorder;
        }
    }
}