// Modelli condivisi con il backend (allineati ai DTO C#).

export enum TipoFornitura { Luce = 0, Gas = 1 }
export enum TipoTariffa { Monoraria = 0, Bioraria = 1, Multioraria = 2 }
export enum FasciaOraria { NonApplicabile = 0, F1 = 1, F2 = 2, F3 = 3 }
export enum CategoriaCosto { MateriaEnergia = 0, TrasportoGestione = 1, OneriDiSistema = 2, Imposte = 3 }
export enum PrioritaSuggerimento { Bassa = 0, Media = 1, Alta = 2 }

export interface Utente {
  id: string;
  email: string;
  nome: string;
  cognome: string;
  telefono?: string | null;
  indirizzo?: string | null;
  citta?: string | null;
  cap?: string | null;
}

export interface AuthResponse {
  token: string;
  scadenzaToken: string;
  utente: Utente;
}

export interface Contratto {
  id: string;
  tipoFornitura: TipoFornitura;
  fornitore: string;
  codicePod?: string | null;
  codicePdr?: string | null;
  nomeOfferta: string;
  tipoTariffa: TipoTariffa;
  potenzaImpegnataKw: number;
  prezzoKwhMonorario: number;
  prezzoKwhF1: number;
  prezzoKwhF2: number;
  prezzoKwhF3: number;
  prezzoSm3: number;
  quotaFissaMensile: number;
  dataAttivazione: string;
  attivo: boolean;
}

export interface VoceDiCosto {
  categoria: CategoriaCosto;
  descrizione: string;
  importo: number;
}

export interface Bolletta {
  id: string;
  contrattoId: string;
  numeroFattura?: string | null;
  periodoInizio: string;
  periodoFine: string;
  dataEmissione?: string | null;
  importoTotale: number;
  consumoTotaleKwh: number;
  consumoF1Kwh: number;
  consumoF2Kwh: number;
  consumoF3Kwh: number;
  consumoSm3: number;
  daOcr: boolean;
  /** Confidenza media (0..1) dell'estrazione OCR; null per inserimenti manuali. */
  confidenzaOcr?: number | null;
  vociDiCosto: VoceDiCosto[];
}

export interface RipartizioneFasce {
  percentualeF1: number;
  percentualeF2: number;
  percentualeF3: number;
}

export interface Suggerimento {
  titolo: string;
  descrizione: string;
  priorita: PrioritaSuggerimento;
  risparmioStimatoAnnuo: number;
  icona: string;
}

export interface AnalisiBolletta {
  bolletta: Bolletta;
  breakdownCosti: VoceDiCosto[];
  ripartizioneFasce: RipartizioneFasce;
  suggerimenti: Suggerimento[];
}

export interface Dispositivo {
  id: string;
  nome: string;
  potenzaWatt: number;
  oreUtilizzoGiornaliere: number;
  giorniSettimana: number;
  fasciaPrevalente: FasciaOraria;
  consumoGiornalieroKwh: number;
  consumoMensileKwh: number;
}

export interface SimulazioneDispositivi {
  consumoTotaleGiornalieroKwh: number;
  consumoTotaleMensileKwh: number;
  costoStimatoMensile: number;
  costoStimatoAnnuo: number;
  dettagli: { nome: string; consumoMensileKwh: number; costoMensile: number }[];
}

export interface DocumentoContrattoInfo {
  contrattoId: string;
  nomeFile: string;
  contentType: string;
  dimensioneByte: number;
  caricatoIl: string;
}

export interface LetturaContatore {
  id: string;
  contrattoId: string;
  dataLettura: string;
  valoreTotale: number;
  valoreF1?: number | null;
  valoreF2?: number | null;
  valoreF3?: number | null;
  daBolletta: boolean;
  note?: string | null;
}

export interface PrevisioneBolletta {
  consumoStimatoKwhOSm3: number;
  giorniPeriodo: number;
  mediaGiornaliera: number;
  costoMateriaPrima: number;
  quotaFissa: number;
  imposteStimate: number;
  importoStimatoTotale: number;
  dataProssimaBollettaStimata: string;
  note: string;
}
