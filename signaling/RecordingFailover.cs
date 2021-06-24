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

            this.recorders.ForEach(async recorder =>
            {
                try
                {
                    await recorder.StopAsync();
                    // TODO properly release resources
                    // recorder.DisconnectAsync() 
                }
                catch(System.Exception e)
                {
                    this.logger.LogError($"Error stoping recording {e.Message}");
                }
            });

            this.recorders.Clear();
        }

        public async Task Setup(string client, KurentoClient kurentoMirror, WebRtcEndpoint receiverEndpoint, MediaPipeline pipeline)
        {
            KurentoMediaServer kms = null;
            var fileName = Guid.NewGuid().ToString();

            for(int i = 1; i <= 2; i++)
            {
                try
                {
                    var mirrorEndpoint = await kurentoMirror.CreateAsync(new WebRtcEndpoint(pipeline));
                    await receiverEndpoint.ConnectAsync(mirrorEndpoint);

                    kms = LoadBalancer.NextAvailable("recorder", kms);
                    var recorderEndpoint = await this.OrchestateReplica(kms, mirrorEndpoint, fileName);
                    this.recorders.Add(recorderEndpoint);
                    this.logger.LogInformation($"client {client} assigned to recording replica {i} - {kms}");
                }
                catch(Exception e)
                {
                    this.logger.LogError($"Failed to setup recorder replica {i}", e);
                }
            }
        }

        private async Task<RecorderEndpoint> OrchestateReplica(KurentoMediaServer kms, WebRtcEndpoint mirrorEndpoint, string fileName)
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

            RecorderEndpoint recorder = await kurento.CreateAsync(new RecorderEndpoint(pipelineRecorder, $"file:///tmp/{fileName}.webm", MediaProfileSpecType.WEBM_VIDEO_ONLY));
            // RecorderEndpoint recorder = await kurento.CreateAsync(new RecorderEndpoint(pipelineRecorder, $"file:///tmp/{DateTime.Now.ToShortTimeString()} [{kms.Name}].webm", MediaProfileSpecType.WEBM_VIDEO_ONLY));
            recorder.Recording += (e) => this.logger.LogInformation("Recording");

            await preRecorderEndpoint.ConnectAsync(recorder, MediaType.VIDEO, "default", "default");

            return recorder;
        }
    }
}