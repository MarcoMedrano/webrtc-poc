using Microsoft.Extensions.Hosting;

using Microsoft.AspNetCore.Hosting;
using NLog.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using System.Threading;

namespace lb_agent
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // var cts = new CancellationTokenSource();
            // var task = CreateHostBuilder(args).Build().RunAsync(cts.Token);
            CreateWebHostBuilder(args).Build().RunAsync();
            CreateHostBuilder(args).Build().Run();
            // var sigintReceived = false;

            // Console.WriteLine("Waiting for SIGINT/SIGTERM");

            // Console.CancelKeyPress += (_, ea) =>
            // {
            //     // Tell .NET to not terminate the process
            //     ea.Cancel = true;

            //     Console.WriteLine("Received SIGINT (Ctrl+C)");
            //     // cts.Cancel();
            //     sigintReceived = true;
            // };

            // AppDomain.CurrentDomain.ProcessExit += (_, _2) =>
            // {
            //     if(!sigintReceived)
            //     {
            //         Console.WriteLine("Received SIGTERM");
            //         // cts.Cancel();
            //     }
            //     else
            //     {
            //         Console.WriteLine("Received SIGTERM, ignoring it because already processed SIGINT");
            //     }
            // };

            // Task.WaitAll(task);
            Console.WriteLine("Good bye");
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<Worker>();
                }).ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
                })
                .UseNLog(); // NLog: Setup NLog for Dependency injection;
                // .UseConsoleLifetime();  

        public static IHostBuilder CreateWebHostBuilder(string[] args) =>
           Host.CreateDefaultBuilder(args)
               .ConfigureWebHostDefaults(webBuilder =>
               {
                   webBuilder.UseUrls("http://*:5000", "https://*:5001");
                   webBuilder.UseStartup<Startup>();
               });
    }
}
