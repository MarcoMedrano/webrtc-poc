using Kurento.NET;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Linq;
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
            try
            {
                this.logger.LogInformation($"Client {this.Context.ConnectionId} disconnected");
                if(this.Context.Items.TryGetValue("recorder", out object recorderObj))
                {
                    var recorder = (RecordingFailover)recorderObj;
                    //also disconnect all associations...
                    await recorder.Stop();
                }

                if(this.Context.Items.TryGetValue("pipeline", out object obj))
                {
                    var pipeline = (MediaPipeline) obj;
                    await pipeline.ReleaseAsync();
                }
                // var pipeline = await endpoint.GetMediaPipelineAsync();
                // if(pipeline != null)
                // {
                //     if(this.Context.Items.TryGetValue("kms", out object obj))
                //     {
                        // var kurento = (KurentoClient)obj;
                        // var refreshedPipeline = await kurento.CreateAsync(pipeline);
                        // this.logger.LogInformation("Releasing pipeline " + await refreshedPipeline.GetNameAsync());
                        // await refreshedPipeline.ReleaseAsync();
                        // this.logger.LogInformation("Released successfully");


                        // var pipelines = await kurento.GetServerManager().GetPipelinesAsync();
                        // this.logger.LogInformation($"Has {pipelines.Length} pipelines");
                        // var attachedPipeline = pipelines.FirstOrDefault(p =>
                        // {
                        //     var t = Task.Run(async () =>
                        //     {
                        //         this.logger.LogInformation($"pipeline - {p}");
                        //         return await p.GetNameAsync();
                        //     });
                        //     Task.WaitAll(t);
                        //     this.logger.LogInformation($"returning  {t.Result} - {this.Context.ConnectionId}");
                        //     return t.Result == this.Context.ConnectionId;
                        // });
                        // this.logger.LogInformation($"Attached pipeline found {await attachedPipeline.GetNameAsync()}");
                        // if(attachedPipeline != null) await attachedPipeline.ReleaseAsync();
                //     }
                // }
                // else
                    // await pipeline.ReleaseAsync();
            }
            catch(System.Exception e)
            {
                this.logger.LogError(e, $"Error disconnecting client {this.Context.ConnectionId}");
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
            try
            {
                this.logger.LogDebug("Adding remote offer \n" /*+ sdpOffer*/);
                var endpoint = await GetKurentoEndpointAsync();
                var sdpAnswer = await endpoint.ProcessOfferAsync(sdpOffer);

                await Clients.Caller.processAnswer(sdpAnswer);

                await endpoint.GatherCandidatesAsync();
            }
            catch(System.Exception e)
            {
                this.logger.LogError(e, $"Error adding offer from client {this.Context.ConnectionId}");

            }
        }

        public async Task AddIceCandidate(string candidateStr /*IceCandidate candidate*/)
        {
            try
            {
                var candidate = JsonConvert.DeserializeObject<IceCandidate>(candidateStr);
                this.logger.LogDebug("Adding remote candidate " + JsonConvert.SerializeObject(candidate));
                var endpoint = await this.GetKurentoEndpointAsync();
                await endpoint.AddIceCandidateAsync(candidate);
            }
            catch(System.Exception e)
            {
                this.logger.LogError(e, $"Error adding ice candidate from client {this.Context.ConnectionId}");
            }

        }
        #endregion

        private async Task<WebRtcEndpoint> GetKurentoEndpointAsync()
        {
            WebRtcEndpoint endpoint = null;
            if(this.Context.Items.TryGetValue("kurento_endpoint", out object endpointObj))
            {
                return (WebRtcEndpoint)endpointObj;
            }

            this.logger.LogInformation("CREATING KURENTO ENDPOINT ");

            var feature = Context.Features.Get<IHttpConnectionFeature>();
            var client = $"{feature.RemoteIpAddress}:{feature.RemotePort}";

            KurentoClient kurento = null;
            if(this.Context.Items.TryGetValue("kms", out object obj))
            {
                kurento = (KurentoClient)obj;
            }
            else
            {
                Cache.MediaServers.ForEach(kms => this.logger.LogDebug("cache " + kms));
                var kms = LoadBalancer.NextAvailable("mirror");
                kurento = kms.KurentoClient;
                this.Context.Items.Add("kms", kurento);

                this.logger.LogInformation($"{client} assigned to MS {kms}");
            }

            var pipeline = await kurento.CreateAsync(new MediaPipeline());
            await pipeline.SetNameAsync(this.Context.ConnectionId);

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
            await this.recorder.Setup(client, kurento, endpoint, pipeline);
            this.recorder.Start();

            this.Context.Items.Add("recorder", recorder);
            this.Context.Items.Add("pipeline", pipeline);
            // endpoint.ElementDisconnected
            return endpoint;
        }
    }
}