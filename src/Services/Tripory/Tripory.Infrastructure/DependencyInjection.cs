using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tripory.Application.Abstractions.Security;
using Tripory.Infrastructure.Configurations;
using Tripory.Infrastructure.Implementations.Security;
using Tripory.Infrastructure.Implementations.Realtime;
using Tripory.Application.Abstractions.Realtime;


namespace Tripory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind JwtOptions
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // HttpContextAccessor
        services.AddHttpContextAccessor();

        // Security Services
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();

        // Realtime Services
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IChatNotificationService, ChatNotificationService>();

        return services;
    }
}
