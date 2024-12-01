using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace TodoTaskAPI.API.Swagger
{
    /// <summary>
    /// Custom schema filter to properly display enums in Swagger
    /// </summary>
    public class EnumSchemaFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type.IsEnum)
            {
                schema.Enum.Clear();
                foreach (var enumName in Enum.GetNames(context.Type))
                {
                    schema.Enum.Add(OpenApiAnyFactory.CreateFromJson(
                        System.Text.Json.JsonSerializer.Serialize(enumName)
                    ));
                }
            }
        }
    }
}
