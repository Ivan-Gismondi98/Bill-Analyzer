using BollettaAnalyzer.Application.Common.Interfaces;
using BollettaAnalyzer.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BollettaAnalyzer.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISuggerimentiService, SuggerimentiService>();
        services.AddScoped<ISimulazioneService, SimulazioneService>();
        return services;
    }
}
