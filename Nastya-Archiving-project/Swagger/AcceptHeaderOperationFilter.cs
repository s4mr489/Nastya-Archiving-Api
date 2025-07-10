using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Threading;
using System.Threading.Tasks;

namespace Nastya_Archiving_project.Swagger
{
    public class AcceptHeaderOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (operation.Parameters == null)
                operation.Parameters = new List<OpenApiParameter>();

            // Add the Accept-Language header parameter
            operation.Parameters.Add(new OpenApiParameter
            {
                Name = "Accept",
                In = ParameterLocation.Header,
                Required = false,
                Schema = new OpenApiSchema
                {
                    Type = "string",
                    Enum = new List<IOpenApiAny>()
                {
                    new OpenApiString("application/json"),
                    new OpenApiString("application/xml"),
                    new OpenApiString("text/plain"),
                },
                    Default = new OpenApiString("application/json")
                }
            });
        }
    }
}
