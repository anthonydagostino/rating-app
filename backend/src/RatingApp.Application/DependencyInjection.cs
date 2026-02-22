using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using RatingApp.Application.Services;
using RatingApp.Application.Validators;

namespace RatingApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<CandidateService>();
        services.AddScoped<IRatingService, RatingService>();
        services.AddScoped<RatingService>();
        services.AddScoped<ChatService>();
        services.AddScoped<PhotoService>();
        services.AddScoped<RatingSessionService>();

        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();

        return services;
    }
}