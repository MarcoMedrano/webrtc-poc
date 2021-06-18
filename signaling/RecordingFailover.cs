using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kurento.NET;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace signaling
{
    public class RecordingFailover
    {
        private readonly ILogger<RecordingFailover> logger;
        private readonly List<RecorderEndpoint> recorders = new List<RecorderEndpoint>();

        public RecordingFailover(ILogger<RecordingFailover> logger)
        {
            this.logger = logger;
        }

        public async Task Start()
        {
            this.logger.LogDebug("Start Recording");
            this.recorders.ForEach(async recorder => recorder.RecordAsync());
        }

        public async Task Stop()
        {
            this.logger.LogDebug("Stop Recording");
            if(this.recorders.Count == 0) this.logger.LogWarning("No recorders to stop");

            this.recorders.ForEach(async recorder => {
                await recorder.StopAsync();
                // TODO properly release resources
                // recorder.DisconnectAsync() 
                });

            this.recorders.Clear();
        }

        public async Task Setup(KurentoClient kurentoMirror, WebRtcEndpoint receiverEndpoint, MediaPipeline pipeline)
        {
            KurentoMediaServer kms = null;

            for(int i = 0; i < 2; i++)
            {
                try {
                    var mirrorEndpoint = await kurentoMirror.CreateAsync(new WebRtcEndpoint(pipeline));
                    await receiverEndpoint.ConnectAsync(mirrorEndpoint);
    
                    kms = LoadBalancer.NextAvailable("recorder", kms);
                    var recorderEndpoint = await this.OrchestateReplica(kms, mirrorEndpoint);
                    this.recorders.Add(recorderEndpoint);
                } catch (Exception e) {
                    this.logger.LogError($"Failed to setup recorder replica", e);
                }
            }
        }

        private async Task<RecorderEndpoint> OrchestateReplica(KurentoMediaServer kms, WebRtcEndpoint mirrorEndpoint)
        {
            var kurento = kms.KurentoClient;
            var pipelineRecorder = await kurento.CreateAsync(new MediaPipeline());
            var preRecorderEndpoint = await kurento.CreateAsync(new WebRtcEndpoint(pipelineRecorder));

            preRecorderEndpoint.OnIceCandidate += async arg =>
            {
                this.logger.LogDebug("Kurento Recorder ice candidate " + JsonConvert.SerializeObject(arg.candidate));
                await mirrorEndpoint.AddIceCandidateAsync(arg.candidate);
            };

            mirrorEndpoint.OnIceCandidate += async arg =>
            {
                this.logger.LogDebug("Kurento Mirror ice candidate " + JsonConvert.SerializeObject(arg.candidate));
                await preRecorderEndpoint.AddIceCandidateAsync(arg.candidate);
            };

            var offer = await mirrorEndpoint.GenerateOfferAsync();
            var answer = await preRecorderEndpoint.ProcessOfferAsync(offer);
            await mirrorEndpoint.ProcessAnswerAsync(answer); //maybe not needed OR preRecorderEndpoint.processAnswerAsync needed as well.

            await mirrorEndpoint.GatherCandidatesAsync();
            await preRecorderEndpoint.GatherCandidatesAsync();

            RecorderEndpoint recorder = await kurento.CreateAsync(new RecorderEndpoint(pipelineRecorder, $"file:///tmp/{DateTime.Now.ToShortTimeString()} [{kms.Ip}].webm", MediaProfileSpecType.WEBM_VIDEO_ONLY));
            recorder.Recording += (e) => this.logger.LogInformation("Recording");

            await preRecorderEndpoint.ConnectAsync(recorder, MediaType.VIDEO, "default", "default");

            return recorder;
        }
    }
}