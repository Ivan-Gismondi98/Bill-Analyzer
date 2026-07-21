using Microsoft.Extensions.Logging;

namespace BollettaAnalyzer.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>();
        // La UI è interamente resa dalla SPA React nell'HybridWebView,
        // quindi non registriamo font MAUI dedicati.

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
