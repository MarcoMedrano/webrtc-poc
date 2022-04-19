using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net;
using System.Net.Sockets;
using Kurento.NET;

namespace lb_agent
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly HubConnection connection;

        private readonly string ip;
        private int availability;
        private bool maintenanceMode;

        private Lazy<KurentoClient> kurentoClient = new Lazy<KurentoClient>(() => new KurentoClient($"ws://localhost:8888/kurento"));
        public KurentoClient KurentoClient => this.kurentoClient.Value;


        public Worker(ILogger<Worker> logger)
        {
            string lb_server = Environment.GetEnvironmentVariable("SIGNALING_SERVER");

            this.logger = logger;
            this.ip = this.GetNetworkIPAddress();
            this.availability = SystemStats.Memory.Available;
            this.maintenanceMode = SystemStats.MaintenanceMode;

            string url = $"http://{lb_server ?? "localhost"}/loadBalancer";

            this.logger.LogInformation("url to connect " + url);

            this.connection = new HubConnectionBuilder()
                            .WithUrl(new Uri(url))
                            .WithAutomaticReconnect(new RetryReconnect())
                            .Build();

            connection.Reconnecting += error =>
            {
                this.logger.LogWarning("Connection lost, current connection state " + this.connection.State);
                return Task.CompletedTask;
            };

            connection.Reconnected += async str =>
            {
                await this.Register();
                this.ReportAvailability();
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await this.ConnectWithRetryAsync(stoppingToken);
            await Task.Delay(10000, stoppingToken);

            await this.Register();
            await this.ReportAvailability();

            while(!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);

                if(this.connection.State != HubConnectionState.Connected) continue;

                try
                {
                    // Only notify if there is substantial changes
                    if(this.availability == SystemStats.Memory.Available
                    && this.maintenanceMode == SystemStats.MaintenanceMode) continue;

                    await this.ReportAvailability();
                    this.availability = SystemStats.Memory.Available;
                    this.maintenanceMode = SystemStats.MaintenanceMode;

                }
                catch(Exception e)
                {
                    this.logger.LogError("Could not report availability due:\n" + e);
                }
            }

            Console.WriteLine("stopping?");

            this.logger.LogInformation("stoping service");

            try
            {
                var serverManager = this.KurentoClient.GetServerManager();
                int pipelines = (await serverManager.GetPipelinesAsync()).Length;

                while(pipelines != 0)
                {
                    this.logger.LogWarning($"Still {pipelines} active, waiting for a gracefull shutdown");
                    await Task.Delay(5000);

                    pipelines = (await serverManager.GetPipelinesAsync()).Length;
                }
            }
            catch(System.Exception e)
            {
                this.logger.LogError(e, "Error checking for available pipelines");
            }

        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("STOPING?");
            this.logger.LogInformation("STOP called ");
            await Task.Delay(15000);
            this.logger.LogInformation("proceeding with stop ");
            await base.StopAsync(cancellationToken);
        }

        private async Task Register()
        {
            string role = Environment.GetEnvironmentVariable("MS_ROLE");
            string name = Environment.GetEnvironmentVariable("MS_NAME");

            this.logger.LogInformation($"Registering with ip {this.ip} role {role} and name {name}");
            await this.connection.InvokeAsync("Register", ip, role, name);
        }

        private async Task ReportAvailability()
        {
            var pipelines = await this.KurentoClient.GetServerManager().GetPipelinesAsync();
            this.logger.LogInformation($"this server still has {pipelines.Length} pipelines");
            // check for CPU, MEMORY, DISK, NETWORK and report availability
            logger.LogInformation($"[{this.ip}] reporting availability {SystemStats.Memory.Available} and Maintenance mode {SystemStats.MaintenanceMode}");
            await connection.InvokeAsync("ReportAvailability", SystemStats.Memory.Available, SystemStats.MaintenanceMode);
        }

        private async Task<bool> ConnectWithRetryAsync(CancellationToken token)
        {
            this.logger.LogInformation("connecting to server");
            while(true)
            {
                try
                {
                    await connection.StartAsync(token);
                    if(connection.State == HubConnectionState.Connected) return true;
                }
                catch when(token.IsCancellationRequested)
                {
                    return false;
                }
                catch
                {
                    this.logger.LogWarning("Failed to connect, trying again in 5000 ms. Connection state " + connection.State);
                    await Task.Delay(5000);
                }
            }
        }

        private string GetNetworkIPAddress()
        {
            // System.Net.NetworkInformation.NetworkInterface.GetIsNetworkAvailable();
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach(var ip in host.AddressList)
            {
                if(ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("No network adapters with an IPv4 address in the system!");
        }
    }

    class RetryReconnect : IRetryPolicy
    {
        public TimeSpan? NextRetryDelay(RetryContext retryContext)
        {
            return TimeSpan.FromSeconds(5);
        }
    }

}