using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR.Client;

namespace lb_agent
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> logger;
        private readonly HubConnection connection;

        public Worker(ILogger<Worker> logger)
        {
            string lb_server = Environment.GetEnvironmentVariable("SIGNALING_SERVER");
            string hub = Environment.GetEnvironmentVariable("MS_ROLE");

            this.logger = logger;
            this.connection = new HubConnectionBuilder()
                            .WithUrl(new Uri($"http://{lb_server}/load_balance{hub}"))
                            .WithAutomaticReconnect()
                            .Build();

            connection.Reconnecting += error =>
            {
                this.logger.LogWarning("Connection lost, current connection state ", this.connection.State);
                return Task.CompletedTask;
            };
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await this.ConnectWithRetryAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                // check for CPU, MEMORY, DISK, NETWORK and report availability
                await connection.InvokeAsync("ReportAvailability", true/*new { bit_rate = 1024 }*/);
                logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                await Task.Delay(1000, stoppingToken);
            }
        }

        private async Task<bool> ConnectWithRetryAsync(CancellationToken token)
        {
            while (true)
            {
                try
                {
                    await connection.StartAsync(token);
                    if (connection.State == HubConnectionState.Connected) return true;
                }
                catch when (token.IsCancellationRequested)
                {
                    return false;
                }
                catch
                {
                    this.logger.LogWarning("Failed to connect, trying again in 5000 ms. Connection state ", connection.State);
                    await Task.Delay(5000);
                }
            }
        }
    }
}
