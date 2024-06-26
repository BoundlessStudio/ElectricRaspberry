using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ElectricRaspberry.Swagger;

public class EnumSchemaFilter : ISchemaFilter
{
    public void Apply(OpenApiSchema schema, SchemaFilterContext context)
    {
        if (context.Type.IsEnum)
        {
            var array = new OpenApiArray();
            array.AddRange(Enum.GetNames(context.Type).Select(n => new OpenApiString(n)));
            schema.Extensions.Add("x-enumNames", array);      // NSwag
            schema.Extensions.Add("x-enum-varnames", array);  // Openapi-generator
        }
    }
}

public static class ExtensionsSwagger
{
    internal static void AddDefaultSwaggerGen(this IServiceCollection services, bool IsDevelopment)
    {
        services.AddSwaggerGen(c =>
        {
            c.SchemaFilter<EnumSchemaFilter>();

            c.SupportNonNullableReferenceTypes();

            c.SwaggerDoc("v1", new()
            {
                Title = "Electric Raspberry",
                Description = "Apis for Volcano Lime",
                Version = "v2",
            });

            if (IsDevelopment)
            {
                c.AddServer(new OpenApiServer()
                {
                    Url = "https://electric-raspberry.ngrok.app",
                    Description = "Development",
                });
            }

            c.AddServer(new OpenApiServer()
            {
                Url = "https://electric-raspberry.azurewebsites.net",
                Description = "Production",
            });

            c.AddSecurityDefinition(
          JwtBearerDefaults.AuthenticationScheme,
          new OpenApiSecurityScheme
          {
              Description = "JWT Authorization header using the Bearer scheme.",
              Type = SecuritySchemeType.Http,
              Scheme = JwtBearerDefaults.AuthenticationScheme
          }
        );

            c.AddSecurityRequirement(
          new OpenApiSecurityRequirement
            {
          {
            new OpenApiSecurityScheme
            {
              Reference = new OpenApiReference
              {
                Id = JwtBearerDefaults.AuthenticationScheme,
                Type = ReferenceType.SecurityScheme
              }
            },
            new List<string>()
          }
            }
        );
        });
    }
}