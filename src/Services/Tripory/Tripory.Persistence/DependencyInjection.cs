using BuildingBlocks.Core.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Tripory.Application.Abstractions.Data;
using Tripory.Domain.Repositories;
using Tripory.Persistence.Repositories;

namespace Tripory.Persistence;

public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Chuỗi kết nối chứa mật khẩu nên không nằm trong appsettings: đặt bằng biến môi trường
        // ConnectionStrings__DefaultConnection hoặc user-secrets khi phát triển.
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Chưa cấu hình ConnectionStrings:DefaultConnection. Đặt biến môi trường ConnectionStrings__DefaultConnection hoặc dùng user-secrets.");

        // Ghi giá trị tham số SQL và lỗi chi tiết vào log chỉ khi cấu hình bật rõ ràng (chỉ Development).
        var enableSensitiveLogging = bool.TryParse(configuration["Persistence:EnableSensitiveDataLogging"], out var enabled)
            && enabled;

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.UseNetTopologySuite(); // Kích hoạt PostGIS spatial support
                npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });
            if (enableSensitiveLogging)
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<IChatMessageRepository, ChatMessageRepository>();
        services.AddScoped<IItineraryRepository, ItineraryRepository>();

        return services;
    }
}
