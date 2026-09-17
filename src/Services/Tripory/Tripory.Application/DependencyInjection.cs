// @TODO: cần xem giải thích file này.
using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Tripory.Application.Behaviors;

namespace Tripory.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // Đăng ký MediatR
        services.AddMediatR(cfg => {
            cfg.RegisterServicesFromAssembly(assembly);

            // Đăng ký validation pipeline behavior
            cfg.AddOpenBehavior(typeof(ValidationPipelineBehavior<,>));
        });

        // Đăng ký tất FluentValidation validators 
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}