namespace ElectricRaspberry.Extensions;

public static class ExtensionsCors
{
    internal static void AddDefaultCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(p =>
          p.WithOrigins("https://volcano-lime.com", "https://volcano-lime.ngrok.app", "https://editor.swagger.io")
           .AllowAnyHeader()
           .AllowAnyMethod()
           .AllowCredentials()
        );
        });
    }
}