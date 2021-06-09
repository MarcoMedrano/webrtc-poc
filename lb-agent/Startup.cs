using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using System.IO;

namespace lb_agent
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public static IConfiguration Configuration { get; private set; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRouting(options => options.LowercaseUrls = true);
            services.AddMvc(options => options.EnableEndpointRouting = false);//.AddMvc(options => options.EnableEndpointRouting = false).SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            // services.AddSwaggerGen(c =>
            //                         {
            //                             c.DocumentFilter<LowercaseDocumentFilter>();
            //                             c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

            //                             var filePath = Path.Combine(System.AppContext.BaseDirectory, "lb-agent.xml");
            //                             c.IncludeXmlComments(filePath);
            //                         });

        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            string prefix = env.IsDevelopment() ? "/dev" : string.Empty;
            string swaggerEndpoint = prefix + "/swagger/v1/swagger.json";
            
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint(swaggerEndpoint, "My API V1");
            });

            app.UseReDoc(c =>
            {
                c.RoutePrefix = "docs";
                c.SpecUrl(swaggerEndpoint);
            });


            app.UseMvc();
        }
    }
}
