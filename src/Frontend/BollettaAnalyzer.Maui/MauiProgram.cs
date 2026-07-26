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

#if ANDROID && DEBUG
        // SOLO in debug: la SPA nell'HybridWebView è servita dall'origine sicura
        // https://0.0.0.1, quindi le chiamate all'API di sviluppo in HTTP
        // (http://10.0.2.2:5080) sono "mixed content" e la WebView Android le
        // blocca a prescindere dai permessi cleartext (ERR_NETWORK dal client).
        // In release il default sicuro resta attivo: l'API di produzione è in HTTPS.
        Microsoft.Maui.Handlers.HybridWebViewHandler.Mapper.AppendToMapping(
            "DevAllowMixedContent", (handler, _) =>
            {
                if (handler.PlatformView is Android.Webkit.WebView webView)
                    webView.Settings.MixedContentMode = Android.Webkit.MixedContentHandling.AlwaysAllow;
            });
#endif

        return builder.Build();
    }
}
