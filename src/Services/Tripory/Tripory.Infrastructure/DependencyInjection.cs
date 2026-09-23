using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tripory.Application.Abstractions.Security;
using Tripory.Infrastructure.Configurations;
using Tripory.Infrastructure.Implementations.Security;
using Tripory.Infrastructure.Implementations.Realtime;
using Tripory.Application.Abstractions.Realtime;
using Tripory.Application.Abstractions.Storage;
using Tripory.Infrastructure.Implementations.Storage;


namespace Tripory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Bind JwtOptions
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        // 2. HttpContextAccessor
        services.AddHttpContextAccessor();

        // 3. Security Services
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // 4. SignalR & Realtime Communication
        services.AddSignalR();
        services.AddScoped<IChatNotificationService, ChatNotificationService>();

        // 5. Audio / File Storage Service
        services.AddScoped<IAudioStorageService, LocalAudioStorageService>();

        return services;
    }
}
