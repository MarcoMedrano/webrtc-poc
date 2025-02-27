using Kurento.NET;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace signaling.hubs
{
    public class RecordingHub : DynamicHub
    {
        private static int recordingNumber = 170;
        private readonly KurentoClient kurento;
        private readonly ILogger<RecordingHub> logger;
        private readonly Settings settings;

        public RecordingHub(KurentoClient kurento, ILogger<RecordingHub> logger, IConfiguration configuration)
        {
            this.kurento = kurento;
            this.logger = logger;
            this.settings = new Settings();

            configuration.GetSection(nameof(Settings)).Bind(this.settings);
        }

        public override async Task OnConnectedAsync()
        {
            this.logger.LogDebug("Client connected with ID " + this.Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public async Task Init() { }

        public async Task Start()
        {
            this.logger.LogDebug("Start Recording");
            if (this.Context.Items.ContainsKey("recorder_endpoint")
            && this.Context.Items.TryGetValue("recorder_endpoint", out object recorderObj))
            {
                RecorderEndpoint recorder = (RecorderEndpoint)recorderObj;

                await recorder.RecordAsync();

            }
        }
        public async Task Stop()
        {
            this.logger.LogDebug("Stop Recording");
            if (this.Context.Items.ContainsKey("recorder_endpoint")
                        && this.Context.Items.TryGetValue("recorder_endpoint", out object recorderObj))
            {
                RecorderEndpoint recorder = (RecorderEndpoint)recorderObj;

                await recorder.StopAsync();
                var endpoint = await GetKurentoEndpointAsync();

                var disconnectTask = recorder.DisconnectAsync(endpoint);
                var createRecorderTask = this.CreateRecorderEndpointAsync(endpoint, await endpoint.GetMediaPipelineAsync());
                
                Task.WaitAll(disconnectTask, createRecorderTask);
            }else{
                this.logger.LogWarning("No recorder_endpoint, did not call START?");
            }
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

        private async Task<WebRtcEndpoint> GetKurentoEndpointAsync()
        {
            WebRtcEndpoint endpoint = null;
            if (this.Context.Items.ContainsKey("kurento_endpoint")
            && this.Context.Items.TryGetValue("kurento_endpoint", out object endpointObj))
            {
                return (WebRtcEndpoint)endpointObj;
            }

            this.logger.LogDebug("CREATING KURENTO ENDPOINT ");

            
            var pipeline = await this.kurento.CreateAsync(new MediaPipeline());

            endpoint = await this.kurento.CreateAsync(new WebRtcEndpoint(pipeline));
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
            await this.CreateRecorderEndpointAsync(endpoint, pipeline);

            return endpoint;
        }

        private async Task<RecorderEndpoint> CreateRecorderEndpointAsync(WebRtcEndpoint endpoint, MediaPipeline pipeline)
        {
            RecorderEndpoint recorder = await this.kurento.CreateAsync(new RecorderEndpoint(pipeline, $"file:///tmp/{recordingNumber++}.webm", MediaProfileSpecType.WEBM_VIDEO_ONLY));
            recorder.Recording += (e) => this.logger.LogInformation("Recording"); 
            
            if(this.Context.Items.ContainsKey("recorder_endpoint")) {
                this.Context.Items.Remove("recorder_endpoint");
            }

            this.Context.Items.Add("recorder_endpoint", recorder);
            await endpoint.ConnectAsync(recorder, MediaType.VIDEO, "default", "default");

            return recorder;
        }
        #endregion
    }
}