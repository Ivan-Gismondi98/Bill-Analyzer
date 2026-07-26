// Dati mock usati quando VITE_USE_MOCK=true o il backend non è raggiungibile.
// Permettono di sviluppare il frontend in autonomia.

import {
  AnalisiBolletta, Bolletta, CategoriaCosto, Contratto, Dispositivo, DocumentoContrattoInfo,
  FasciaOraria, LetturaContatore, PrioritaSuggerimento, TipoFornitura, TipoTariffa, Utente,
} from '../types';

export const mockUtente: Utente = {
  id: '00000000-0000-0000-0000-000000000001',
  email: 'demo@bolletta.app',
  nome: 'Mario',
  cognome: 'Rossi',
  telefono: '+39 333 1234567',
  indirizzo: 'Via Roma 1',
  citta: 'Milano',
  cap: '20100',
};

export const mockContratti: Contratto[] = [
  {
    id: 'c0000000-0000-0000-0000-000000000001',
    tipoFornitura: TipoFornitura.Luce,
    fornitore: 'Enel Energia',
    codicePod: 'IT001E12345678',
    codicePdr: null,
    nomeOfferta: 'Luce Web Trioraria',
    tipoTariffa: TipoTariffa.Multioraria,
    potenzaImpegnataKw: 3.0,
    prezzoKwhMonorario: 0.145,
    prezzoKwhF1: 0.17,
    prezzoKwhF2: 0.15,
    prezzoKwhF3: 0.12,
    prezzoSm3: 0,
    quotaFissaMensile: 9.5,
    dataAttivazione: '2024-01-15T00:00:00Z',
    attivo: true,
  },
  {
    id: 'c0000000-0000-0000-0000-000000000002',
    tipoFornitura: TipoFornitura.Gas,
    fornitore: 'Eni Plenitude',
    codicePod: null,
    codicePdr: '01234567890123',
    nomeOfferta: 'Gas Trend Casa',
    tipoTariffa: TipoTariffa.Monoraria,
    potenzaImpegnataKw: 0,
    prezzoKwhMonorario: 0,
    prezzoKwhF1: 0,
    prezzoKwhF2: 0,
    prezzoKwhF3: 0,
    prezzoSm3: 0.52,
    quotaFissaMensile: 8.0,
    dataAttivazione: '2024-01-15T00:00:00Z',
    attivo: true,
  },
];

export const mockBollette: Bolletta[] = [
  {
    id: 'b0000000-0000-0000-0000-000000000001',
    contrattoId: mockContratti[0].id,
    numeroFattura: '2026/000123',
    periodoInizio: '2026-03-01T00:00:00Z',
    periodoFine: '2026-04-30T00:00:00Z',
    dataEmissione: '2026-05-08T00:00:00Z',
    importoTotale: 98.74,
    consumoTotaleKwh: 412,
    consumoF1Kwh: 198,
    consumoF2Kwh: 92,
    consumoF3Kwh: 122,
    consumoSm3: 0,
    daOcr: false,
    vociDiCosto: [
      { categoria: CategoriaCosto.MateriaEnergia, descrizione: 'Spesa per la materia energia', importo: 58.2 },
      { categoria: CategoriaCosto.TrasportoGestione, descrizione: 'Trasporto e gestione contatore', importo: 17.3 },
      { categoria: CategoriaCosto.OneriDiSistema, descrizione: 'Oneri di sistema', importo: 12.4 },
      { categoria: CategoriaCosto.Imposte, descrizione: 'Imposte e IVA', importo: 10.84 },
    ],
  },
];

export const mockAnalisi: AnalisiBolletta = {
  bolletta: mockBollette[0],
  breakdownCosti: mockBollette[0].vociDiCosto,
  ripartizioneFasce: { percentualeF1: 48.1, percentualeF2: 22.3, percentualeF3: 29.6 },
  suggerimenti: [
    {
      titolo: 'Costo materia prima sopra la media',
      descrizione: 'Paghi 0,170 €/kWh in F1 contro una media di mercato di 0,130 €/kWh. Valuta un\'offerta più conveniente.',
      priorita: PrioritaSuggerimento.Alta,
      risparmioStimatoAnnuo: 120,
      icona: 'trending-down',
    },
    {
      titolo: 'Sposta i consumi nelle fasce economiche',
      descrizione: 'Il 48% dei consumi è in fascia F1. Programma lavatrice e lavastoviglie in F3 (sera/notte e festivi).',
      priorita: PrioritaSuggerimento.Media,
      risparmioStimatoAnnuo: 45,
      icona: 'clock',
    },
  ],
};

// Documenti di contratto cifrati (metadati). Mutabile: aggiornato dalle chiamate mock.
export const mockDocumenti: Record<string, DocumentoContrattoInfo> = {};

// Storico auto-letture (max 3 per contratto). Mutabile.
export const mockLetture: LetturaContatore[] = [
  { id: 'l1', contrattoId: mockContratti[0].id, dataLettura: '2026-04-30T00:00:00Z', valoreTotale: 15420, valoreF1: 7100, valoreF2: 4200, valoreF3: 4120, daBolletta: true, note: 'Lettura di chiusura ultima bolletta' },
  { id: 'l2', contrattoId: mockContratti[0].id, dataLettura: '2026-05-31T00:00:00Z', valoreTotale: 15588, valoreF1: null, valoreF2: null, valoreF3: null, daBolletta: false, note: 'Auto-lettura mensile' },
  { id: 'l3', contrattoId: mockContratti[0].id, dataLettura: '2026-06-20T00:00:00Z', valoreTotale: 15680, valoreF1: null, valoreF2: null, valoreF3: null, daBolletta: false, note: null },
];

export const mockDispositivi: Dispositivo[] = [
  { id: 'd1', nome: 'Frigorifero', potenzaWatt: 150, oreUtilizzoGiornaliere: 24, giorniSettimana: 7, fasce: [FasciaOraria.F1, FasciaOraria.F2, FasciaOraria.F3], consumoGiornalieroKwh: 3.6, consumoMensileKwh: 108 },
  { id: 'd2', nome: 'Lavatrice', potenzaWatt: 2000, oreUtilizzoGiornaliere: 1.5, giorniSettimana: 4, fasce: [FasciaOraria.F1], consumoGiornalieroKwh: 3, consumoMensileKwh: 51.4 },
  { id: 'd3', nome: 'Forno elettrico', potenzaWatt: 2200, oreUtilizzoGiornaliere: 0.5, giorniSettimana: 5, fasce: [FasciaOraria.F2], consumoGiornalieroKwh: 1.1, consumoMensileKwh: 23.6 },
  { id: 'd4', nome: 'Condizionatore', potenzaWatt: 1200, oreUtilizzoGiornaliere: 4, giorniSettimana: 6, fasce: [FasciaOraria.F1, FasciaOraria.F2], consumoGiornalieroKwh: 4.8, consumoMensileKwh: 123.4 },
];
