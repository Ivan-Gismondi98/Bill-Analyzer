using System.Diagnostics;

namespace BollettaAnalyzer.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
#if DEBUG
        _ = VerificaAssetClientAsync();
#endif
    }

#if DEBUG
    /// <summary>
    /// Diagnostica di sviluppo: verifica che la SPA React sia effettivamente
    /// impacchettata come app package file. Se manca, l'HybridWebView mostra
    /// "Resource not found (404)" e qui si vede subito il perché nell'Output.
    /// </summary>
    private static async Task VerificaAssetClientAsync()
    {
        const string percorso = "wwwroot/index.html";
        try
        {
            using var stream = await FileSystem.OpenAppPackageFileAsync(percorso);
            using var reader = new StreamReader(stream);
            var contenuto = await reader.ReadToEndAsync();
            Debug.WriteLine($"[BollettaAnalyzer] OK: '{percorso}' trovato nel pacchetto ({contenuto.Length} caratteri).");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(
                $"[BollettaAnalyzer] ERRORE: '{percorso}' NON è nel pacchetto dell'app ({ex.GetType().Name}). " +
                "La build del client React non è stata impacchettata: esegui 'npm run build' in ClientApp, " +
                "poi ricompila e reinstalla l'app (disinstallala prima dal dispositivo).");
        }
    }
#endif
}
