using System;
using System.Threading.Tasks;
using Kurento.NET;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace signaling.hubs
{
    public class MirrorLoadBalancerHub : DynamicHub
    {
        private readonly ILogger<MirrorLoadBalancerHub> logger;
        private readonly IConfiguration configuration;

        public MirrorLoadBalancerHub(ILogger<MirrorLoadBalancerHub> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        public void ReportAvailability(long available)
        {
            KurentoMediaServer kms = null;
            if(this.Context.Items.TryGetValue("kms", out object obj))
            {
                kms = (KurentoMediaServer)obj;
                kms.Available = available;
                this.logger.LogInformation($"KMS {kms} reported availability for {available}Mb");
            }
            else
                this.logger.LogError("No media server found");
        }

        public void ReportMaintenanceMode(bool maintenance)
        {
            KurentoMediaServer kms = null;
            if(this.Context.Items.TryGetValue("kms", out object obj))
            {
                kms = (KurentoMediaServer)obj;
                kms.MaintenanceMode = maintenance;
                this.logger.LogInformation($"KMS {kms} reported entering into maintenance mode {maintenance}");
            }
            else
                this.logger.LogError("No media server found");
        }

        public override async Task OnConnectedAsync()
        {
            var feature = Context.Features.Get<IHttpConnectionFeature>();
            var kms = new KurentoMediaServer(feature.RemoteIpAddress.ToString(), feature.RemotePort);
            this.logger.LogInformation($"Client connected {kms}");

            if(Cache.MirrorMediaServers.Contains(kms))
                this.logger.LogWarning($"Media Server {kms} already exist in cache");
            else
                Cache.MirrorMediaServers.Add(kms);

            if(this.Context.Items.ContainsKey("kms"))
                this.logger.LogWarning($"Media Server {kms} already exist in context");
            else
                this.Context.Items.Add("kms", kms);

            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var feature = Context.Features.Get<IHttpConnectionFeature>();
            var kms = new KurentoMediaServer(feature.RemoteIpAddress.ToString(), feature.RemotePort);

            this.logger.LogWarning($"Client disconnected {kms}");

            if(Cache.MirrorMediaServers.Contains(kms))
                Cache.MirrorMediaServers.Remove(kms);
            else
                this.logger.LogWarning($"Media Server {kms} did not exist in cache");

            if(this.Context.Items.ContainsKey("kms"))
                this.Context.Items.Remove("kms");
            else
                this.logger.LogWarning($"Media Server {kms} did not exist in context");

            return base.OnDisconnectedAsync(exception);
        }
    }
}