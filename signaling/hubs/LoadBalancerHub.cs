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

        public void Register(string ip, string role, string name)
        {
            try
            {
                var feature = Context.Features.Get<IHttpConnectionFeature>();
                var kms = new KurentoMediaServer(this.Context.ConnectionId, ip, feature.RemotePort, role, name);
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
            catch(System.Exception e)
            {
                this.logger.LogError(e, $"Error registering kms {this.Context.ConnectionId}");
            }
        }

        public async void ReportAvailability(long available, bool maintenanceMode)
        {
            try
            {
                KurentoMediaServer kms = null;
                if(this.Context.Items.TryGetValue("kms", out object obj))
                {
                    kms = (KurentoMediaServer)obj;
                    kms.Available = available;
                    kms.MaintenanceMode = maintenanceMode;
                    this.logger.LogInformation($"KMS {kms} reported availability for {available} Mb and maintenance mode {maintenanceMode}");

                    if(true || maintenanceMode)
                    {
                        var pipelines = await kms.KurentoClient.GetServerManager().GetPipelinesAsync();
                        this.logger.LogInformation($"KMS {kms}  has {pipelines.Length} pipelines");
                    }
                }
                else
                    this.logger.LogError("No media server found");
            }
            catch(System.Exception e)
            {
                this.logger.LogError(e, $"Error reporting availability from kms {this.Context.ConnectionId}");
            }
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            try
            {
                var feature = Context.Features.Get<IHttpConnectionFeature>();
                var kms = Cache.MediaServers.Find(kms => kms.ConnectionId == this.Context.ConnectionId);

                this.logger.LogWarning($"Client disconnected {kms}");

                if(kms != null)
                    Cache.MediaServers.Remove(kms);
                else
                    this.logger.LogWarning($"Media Server {this.Context.ConnectionId } did not exist in cache");

                if(this.Context.Items.ContainsKey("kms"))
                    this.Context.Items.Remove("kms");
                else
                    this.logger.LogWarning($"Media Server {this.Context.ConnectionId } did not exist in context");
            }
            catch(System.Exception e)
            {
                this.logger.LogError(e, $"Error disconnecting kms {this.Context.ConnectionId}");
            }

            return base.OnDisconnectedAsync(exception);
        }
    }
}