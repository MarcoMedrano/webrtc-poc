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
        private readonly KurentoClient kurento;
        private readonly ILogger<MirrorLoadBalancerHub> logger;
        private readonly IConfiguration configuration;

        public MirrorLoadBalancerHub(KurentoClient kurento, ILogger<MirrorLoadBalancerHub> logger, IConfiguration configuration)
        {
            this.kurento = kurento;
            this.logger = logger;
            this.configuration = configuration;
        }

        public override async Task OnConnectedAsync()
        {
            var feature = Context.Features.Get<IHttpConnectionFeature>();
            this.logger.LogInformation($"Client connected with IP {feature.RemoteIpAddress}:{feature.RemotePort}");

            this.logger.LogDebug("Client connected with ID " + this.Context.ConnectionId);
            await base.OnConnectedAsync();
        }
    }
}