using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tripory.Application.Abstractions.Security;
using Tripory.Infrastructure.Configurations;
using Tripory.Infrastructure.Implementations.Security;
using Tripory.Infrastructure.Implementations.Realtime;
using Tripory.Application.Abstractions.Realtime;
using Tripory.Application.Abstractions.DestinationCover;
using Tripory.Application.Abstractions.Storage;
using Tripory.Domain.Abstractions.External;
using Tripory.Infrastructure.Implementations.Gis;
using Tripory.Infrastructure.Implementations.Storage;
using Tripory.Infrastructure.Implementations.DestinationCover;


namespace Tripory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. Bind JwtOptions
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => o.SecretKey.Length >= JwtOptions.MinSecretKeyLength,
                $"JwtOptions: SecretKey phải có ít nhất {JwtOptions.MinSecretKeyLength} ký tự. Đặt bằng biến môi trường JwtOptions__SecretKey hoặc user-secrets.")
            .Validate(o => o.ExpiryMinutes > 0,
                "JwtOptions: ExpiryMinutes phải lớn hơn 0.")
            .Validate(o => o.RefreshTokenExpiryDays > 0,
                "JwtOptions: RefreshTokenExpiryDays phải lớn hơn 0.")
            .ValidateOnStart();

        // 2. Bind DestinationCatalogOptions
        services.AddOptions<DestinationCatalogOptions>()
            .Bind(configuration.GetSection(DestinationCatalogOptions.SectionName))
            .Validate(o => DestinationCatalogOptions.IsAbsoluteHttpsUrl(o.CoverImageBaseUrl),
                "DestinationCatalogOptions: CoverImageBaseUrl phải là một URL tuyệt đối dùng https, không có khoảng trắng, '?' hay '#'."
            )
            .Validate(o => o.CoverImageBaseUrl?.EndsWith('/') == true,
                "DestinationCatalogOptions: CoverImageBaseUrl phải kết thúc bằng ký tự '/'."
            )
            .Validate(o => o.CoverImageBaseUrl?.Length <= DestinationCatalogOptions.MaxBaseUrlLength,
                $"DestinationCatalogOptions: CoverImageBaseUrl không được dài quá {DestinationCatalogOptions.MaxBaseUrlLength} ký tự."
            )
            .ValidateOnStart();

        // 3. HttpContextAccessor
        services.AddHttpContextAccessor();

        // 4. Security Services
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // 5. SignalR & Realtime Communication
        services.AddSignalR();
        services.AddScoped<IChatNotificationService, ChatNotificationService>();

        // 6. Audio / File Storage Service
        services.AddScoped<IAudioStorageService, LocalAudioStorageService>();

        // 7. GIS & Geodesic Distance Service
        services.AddSingleton<IGisDistanceCalculator, GisDistanceCalculator>();

        // 8. Destination cover image Service
        services.AddSingleton<IDestinationCoverProvider, DestinationCoverProvider>();

        return services;
    }
}
