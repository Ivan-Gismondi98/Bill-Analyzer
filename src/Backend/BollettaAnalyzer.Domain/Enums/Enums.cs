namespace BollettaAnalyzer.Domain.Enums;

/// <summary>Tipo di fornitura energetica.</summary>
public enum TipoFornitura
{
    Luce = 0,
    Gas = 1
}

/// <summary>Struttura tariffaria del contratto luce.</summary>
public enum TipoTariffa
{
    /// <summary>Prezzo unico dell'energia in qualsiasi ora del giorno.</summary>
    Monoraria = 0,

    /// <summary>Prezzo differenziato per due fasce (F1 / F23).</summary>
    Bioraria = 1,

    /// <summary>Prezzo differenziato per tre fasce (F1 / F2 / F3).</summary>
    Multioraria = 2
}

/// <summary>
/// Fasce orarie ARERA. Per il gas si usa <see cref="NonApplicabile"/>.
/// F1 = ore di punta, F2 = ore intermedie, F3 = ore fuori punta/festivi.
/// </summary>
public enum FasciaOraria
{
    NonApplicabile = 0,
    F1 = 1,
    F2 = 2,
    F3 = 3
}

/// <summary>Macro-categorie di costo che compongono la spesa di una bolletta (classificazione ARERA).</summary>
public enum CategoriaCosto
{
    /// <summary>Spesa per la materia energia / gas naturale.</summary>
    MateriaEnergia = 0,

    /// <summary>Spesa per il trasporto e la gestione del contatore.</summary>
    TrasportoGestione = 1,

    /// <summary>Oneri di sistema.</summary>
    OneriDiSistema = 2,

    /// <summary>Imposte, accise e IVA.</summary>
    Imposte = 3
}
