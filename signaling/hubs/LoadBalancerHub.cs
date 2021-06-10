using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace signaling.hubs
{
    public class LoadBalancerHub : DynamicHub
    {
        private readonly ILogger<LoadBalancerHub> logger;
        private readonly IConfiguration configuration;

        public LoadBalancerHub(ILogger<LoadBalancerHub> logger, IConfiguration configuration)
        {
            this.logger = logger;
            this.configuration = configuration;
        }

        public void Register(string ip, string role)
        {
            var feature = Context.Features.Get<IHttpConnectionFeature>();
            var kms = new KurentoMediaServer(ip, feature.RemotePort, role);
            this.logger.LogInformation($"Agent connected {kms}");

            if(Cache.MediaServers.Contains(kms))
                this.logger.LogWarning($"Media Server {kms} already exist in cache");
            else
                Cache.MediaServers.Add(kms);

            if(this.Context.Items.ContainsKey("kms"))
                this.logger.LogWarning($"Media Server {kms} already exist in context");
            else
                this.Context.Items.Add("kms", kms);
        }

        public void ReportAvailability(long available)
        {
            KurentoMediaServer kms = null;
            if(this.Context.Items.TryGetValue("kms", out object obj))
            {
                kms = (KurentoMediaServer)obj;
                kms.Available = available;
                this.logger.LogInformation($"KMS {kms} reported availability for {available} Mb");
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

        public override Task OnDisconnectedAsync(Exception exception)
        {
            var feature = Context.Features.Get<IHttpConnectionFeature>();
            var kms = new KurentoMediaServer(feature.RemoteIpAddress.ToString(), feature.RemotePort);

            this.logger.LogWarning($"Client disconnected {kms}");

            if(Cache.MediaServers.Contains(kms))
                Cache.MediaServers.Remove(kms);
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