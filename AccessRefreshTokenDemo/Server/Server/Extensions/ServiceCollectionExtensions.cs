using Microsoft.EntityFrameworkCore;
using Server.Data;
using Server.Middleware;
using Server.Services;
using Server.Services.Interfaces;

namespace Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddExceptionHandler<ExceptionMiddleware>();
        services.AddProblemDetails();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    public static IServiceCollection AddDb(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DbConnection"))
        );
        return services;
    }
}
