using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Server.Data;
using Server.Middleware;
using Server.Services;
using Server.Services.Interfaces;

namespace Server.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAllServices(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddServices();
        services.AddCustomMiddleware(configuration);
        services.AddDb(configuration);
        return services;
    }

    public static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddControllers();
        services.AddOpenApi();
        services.AddProblemDetails();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }

    public static IServiceCollection AddCustomMiddleware(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddExceptionHandler<ExceptionMiddleware>();
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new()
                {
                    ValidateIssuer = true,
                    ValidIssuers = [configuration.GetValue<string>("JWT:Issuer")],
                    ValidateAudience = true,
                    ValidAudiences = [configuration.GetValue<string>("JWT:Audience")],
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration.GetValue<string>("JWT:Key")!)
                    ),
                };
            });
        services.AddCors(options =>
            options.AddDefaultPolicy(policy =>
                policy
                    .WithOrigins("http://localhost:5173")
                    .WithMethods("GET", "POST", "PUT", "DELETE")
                    .AllowAnyHeader()
                    .AllowCredentials()
            )
        );
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
