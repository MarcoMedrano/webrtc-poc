using System;
using System.Threading.Tasks;
using Kurento.NET;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace signaling
{
    public class RecordingFailover
    {
        private readonly ILogger<RecordingFailover> logger;

        public RecordingFailover(ILogger<RecordingFailover> logger)
        {
            this.logger = logger;
        }

        public async Task CreateRecorderEndpointAsync(KurentoClient kurentoMirror, WebRtcEndpoint receiverEndpoint, MediaPipeline pipeline)
        {
            KurentoMediaServer kms = null;

            for(int i = 0; i < 2; i++)
            {
                var mirrorEndpoint = await kurentoMirror.CreateAsync(new WebRtcEndpoint(pipeline));
                await receiverEndpoint.ConnectAsync(mirrorEndpoint);

                kms = LoadBalancer.NextAvailable("recorder", kms);
                await this.OrchestateReplica(kms, mirrorEndpoint);
            }
        }

        private async Task OrchestateReplica(KurentoMediaServer kms, WebRtcEndpoint mirrorEndpoint)
        {
            var kurento = kms.KurentoClient;
            var pipelineRecorder = await kurento.CreateAsync(new MediaPipeline());
            var preRecorderEndpoint = await kurento.CreateAsync(new WebRtcEndpoint(pipelineRecorder));

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

            RecorderEndpoint recorder = await kurento.CreateAsync(new RecorderEndpoint(pipelineRecorder, $"file:///tmp/{DateTime.Now.ToShortTimeString()} [{kms.Ip}].webm", MediaProfileSpecType.WEBM_VIDEO_ONLY));
            recorder.Recording += (e) => this.logger.LogInformation("Recording");

            await preRecorderEndpoint.ConnectAsync(recorder, MediaType.VIDEO, "default", "default");

            await recorder.RecordAsync();
        }
    }
}