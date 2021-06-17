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

namespace lb_agent
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly HubConnection connection;

        private readonly string ip;
        private readonly int availability;
        private readonly bool maintenanceMode;

        public Worker(ILogger<Worker> logger)
        {
            string lb_server = Environment.GetEnvironmentVariable("SIGNALING_SERVER");

            this.logger = logger;
            this.ip = this.GetNetworkIPAddress();
            this.availability = SystemStats.Memory.Available;
            this.maintenanceMode = SystemStats.MaintenanceMode;

            string url = $"http://{lb_server}/loadBalancer";

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

            while(!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(30000, stoppingToken);

                if(this.connection.State != HubConnectionState.Connected) continue;

                try
                {
                    // Only notify if there is substantial changes
                    if(this.availability == SystemStats.Memory.Available
                    && this.maintenanceMode == SystemStats.MaintenanceMode) continue;
                    this.ReportAvailability();

                }
                catch(Exception e)
                {
                    this.logger.LogError("Could not report availability due:\n" + e);
                }
            }
        }

        private async Task Register()
        {
            string role = Environment.GetEnvironmentVariable("MS_ROLE");

            this.logger.LogInformation($"Registering with ip {this.ip} and role {role}");
            await this.connection.InvokeAsync("Register", ip, role);
        }

        private async Task ReportAvailability()
        {
            // check for CPU, MEMORY, DISK, NETWORK and report availability
            logger.LogInformation($"[{this.ip}] reporting availability {SystemStats.Memory.Available}");
            await connection.InvokeAsync("ReportAvailability", SystemStats.Memory.Available);
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