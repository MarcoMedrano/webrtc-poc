using System.Linq;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.Swagger;
using Microsoft.OpenApi.Models;

namespace lb_agent
{

    public class LowercaseDocumentFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            var oldPaths = swaggerDoc.Paths;
            swaggerDoc.Paths = new OpenApiPaths();
            foreach(var kvp in oldPaths){
                swaggerDoc.Paths.Add(LowercaseEverythingButParameters(kvp.Key), kvp.Value);
            }
        }

        private static string LowercaseEverythingButParameters(string key)
        {
            return string.Join('/', key.Split('/').Select(x => x.Contains("{") ? x : x.ToLower()));
        }
    }
}