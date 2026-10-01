using BookingHubAPI.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace BookingHubAPI.Application;

public static class DependencyInjection
{
    /// <summary>Registers the application services and every FluentValidation validator of this assembly.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.TryAddSingleton(TimeProvider.System);
        services.AddValidatorsFromAssemblyContaining<IReservationService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IServiceCatalogService, ServiceCatalogService>();
        services.AddScoped<IWorkingHoursService, WorkingHoursService>();
        return services;
    }
}
