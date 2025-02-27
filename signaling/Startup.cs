using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;
using Kurento.NET;
using signaling.hubs;

namespace signaling
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton(p => new KurentoClient("ws://172.31.34.191:8888/kurento"));
            services.AddScoped<RecordingFailover>();

            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.WithOrigins("http://localhost:3000", "https://localhost:3000", "file://").AllowCredentials().AllowAnyHeader();
                });

            });

            services.AddSignalR();//.AddMessagePackProtocol();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if(env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hey!");
                });

                endpoints.MapHub<RecordingHub>("/recording");
                endpoints.MapHub<LiveMonitoringHub>("/liveMonitoring");
                endpoints.MapHub<SignalingHub>("/signaling");
                endpoints.MapHub<LoadBalancerHub>("/loadBalancer");
                endpoints.MapHub<RecorderLoadBalancerHub>("/recorderLoadBalancer");
            });
        }
    }
}
