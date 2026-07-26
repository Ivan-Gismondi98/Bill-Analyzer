using System.Diagnostics;

namespace BollettaAnalyzer.Maui;

public partial class MainPage : ContentPage
{
    /// <summary>Percorso della SPA dentro il pacchetto (HybridRoot + DefaultFile).</summary>
    private const string PercorsoIndex = "wwwroot/index.html";

    public MainPage()
    {
        InitializeComponent();
        _ = VerificaClientAsync();
    }

    /// <summary>
    /// Verifica che la SPA React sia effettivamente presente tra gli asset del pacchetto.
    /// Se manca, l'HybridWebView mostrerebbe solo un anonimo "Resource not found (404)":
    /// qui invece si spiega a schermo la causa e come rimediare.
    /// </summary>
    private async Task VerificaClientAsync()
    {
        try
        {
            await using var stream = await FileSystem.OpenAppPackageFileAsync(PercorsoIndex);
            using var reader = new StreamReader(stream);
            var html = await reader.ReadToEndAsync();

            Debug.WriteLine($"[BollettaAnalyzer] OK: '{PercorsoIndex}' presente nel pacchetto ({html.Length} caratteri).");
        }
        catch (Exception ex)
        {
            var messaggio =
                $"'{PercorsoIndex}' non è presente tra gli asset dell'app ({ex.GetType().Name}). " +
                "La build del client React non è stata impacchettata nell'APK.";

            Debug.WriteLine($"[BollettaAnalyzer] ERRORE: {messaggio}");

            // L'UI va aggiornata sul thread principale.
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                dettaglioErrore.Text = messaggio;
                pannelloErrore.IsVisible = true;
                webView.IsVisible = false;
            });
        }
    }
}
