import axios, { AxiosInstance } from 'axios';
import {
  AnalisiBolletta, AuthResponse, Bolletta, Contratto, Dispositivo, DocumentoContrattoInfo,
  FasciaOraria, LetturaContatore, PrevisioneBolletta, SimulazioneDispositivi, TipoFornitura, TipoTariffa, Utente,
} from '../types';
import * as mock from './mockData';

// Con VITE_USE_MOCK=true l'app funziona senza backend (utile in sviluppo UI).
const USE_MOCK = import.meta.env.VITE_USE_MOCK === 'true';
const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api';

const TOKEN_KEY = 'ba_token';

export const tokenStore = {
  get: () => localStorage.getItem(TOKEN_KEY),
  set: (t: string) => localStorage.setItem(TOKEN_KEY, t),
  clear: () => localStorage.removeItem(TOKEN_KEY),
};

const http: AxiosInstance = axios.create({ baseURL: BASE_URL });
http.interceptors.request.use((config) => {
  const token = tokenStore.get();
  if (token) config.headers.Authorization = `Bearer ${token}`;
  return config;
});

const delay = (ms = 300) => new Promise((r) => setTimeout(r, ms));

// --- Auth ---
export const authApi = {
  async login(email: string, password: string): Promise<AuthResponse> {
    if (USE_MOCK) {
      await delay();
      return { token: 'mock-token', scadenzaToken: '2099-01-01T00:00:00Z', utente: mock.mockUtente };
    }
    const { data } = await http.post<AuthResponse>('/auth/login', { email, password });
    return data;
  },
  async register(email: string, password: string, nome: string, cognome: string): Promise<AuthResponse> {
    if (USE_MOCK) {
      await delay();
      return { token: 'mock-token', scadenzaToken: '2099-01-01T00:00:00Z', utente: { ...mock.mockUtente, email, nome, cognome } };
    }
    const { data } = await http.post<AuthResponse>('/auth/register', { email, password, nome, cognome });
    return data;
  },
  async me(): Promise<Utente> {
    if (USE_MOCK) { await delay(); return mock.mockUtente; }
    const { data } = await http.get<Utente>('/auth/me');
    return data;
  },
  async updateProfilo(payload: Partial<Utente>): Promise<Utente> {
    if (USE_MOCK) { await delay(); return { ...mock.mockUtente, ...payload }; }
    const { data } = await http.put<Utente>('/auth/profilo', payload);
    return data;
  },
};

// --- Contratti ---
export const contrattiApi = {
  async lista(): Promise<Contratto[]> {
    if (USE_MOCK) { await delay(); return mock.mockContratti; }
    const { data } = await http.get<Contratto[]>('/contratti');
    return data;
  },
  async salva(payload: Partial<Contratto>, id?: string): Promise<Contratto> {
    if (USE_MOCK) { await delay(); return { ...mock.mockContratti[0], ...payload, id: id ?? crypto.randomUUID() }; }
    const { data } = id
      ? await http.put<Contratto>(`/contratti/${id}`, payload)
      : await http.post<Contratto>('/contratti', payload);
    return data;
  },

  // --- Documento di contratto (PDF cifrato a riposo) ---
  async documentoInfo(id: string): Promise<DocumentoContrattoInfo | null> {
    if (USE_MOCK) { await delay(); return mock.mockDocumenti[id] ?? null; }
    try {
      const { data } = await http.get<DocumentoContrattoInfo>(`/contratti/${id}/documento`);
      return data;
    } catch {
      return null; // 404 = nessun documento
    }
  },
  async caricaDocumento(id: string, file: File): Promise<DocumentoContrattoInfo> {
    if (USE_MOCK) {
      await delay(600);
      const info: DocumentoContrattoInfo = {
        contrattoId: id, nomeFile: file.name, contentType: file.type || 'application/pdf',
        dimensioneByte: file.size, caricatoIl: new Date().toISOString(),
      };
      mock.mockDocumenti[id] = info;
      return info;
    }
    const form = new FormData();
    form.append('file', file);
    const { data } = await http.post<DocumentoContrattoInfo>(`/contratti/${id}/documento`, form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return data;
  },
  async eliminaDocumento(id: string): Promise<void> {
    if (USE_MOCK) { await delay(); delete mock.mockDocumenti[id]; return; }
    await http.delete(`/contratti/${id}/documento`);
  },
  async scaricaDocumento(id: string): Promise<Blob> {
    if (USE_MOCK) { await delay(); return new Blob(['%PDF-1.4 mock'], { type: 'application/pdf' }); }
    const { data } = await http.get(`/contratti/${id}/documento/download`, { responseType: 'blob' });
    return data as Blob;
  },
};

// --- Letture contatore (storico ultime 3) ---
export const lettureApi = {
  async lista(contrattoId: string): Promise<LetturaContatore[]> {
    if (USE_MOCK) { await delay(); return mock.mockLetture.filter((l) => l.contrattoId === contrattoId).slice(0, 3); }
    const { data } = await http.get<LetturaContatore[]>('/letture', { params: { contrattoId } });
    return data;
  },
  async crea(payload: {
    contrattoId: string; dataLettura: string; valoreTotale: number;
    valoreF1?: number | null; valoreF2?: number | null; valoreF3?: number | null; note?: string | null;
  }): Promise<LetturaContatore> {
    if (USE_MOCK) {
      await delay();
      const nuova: LetturaContatore = { id: crypto.randomUUID(), daBolletta: false, ...payload };
      mock.mockLetture.unshift(nuova);
      mock.mockLetture.splice(3); // mantiene solo le ultime 3
      return nuova;
    }
    const { data } = await http.post<LetturaContatore>('/letture', payload);
    return data;
  },
};

// --- Bollette ---
export const bolletteApi = {
  async lista(contrattoId?: string): Promise<Bolletta[]> {
    if (USE_MOCK) { await delay(); return mock.mockBollette; }
    const { data } = await http.get<Bolletta[]>('/bollette', { params: { contrattoId } });
    return data;
  },
  async analisi(id: string): Promise<AnalisiBolletta> {
    if (USE_MOCK) { await delay(); return mock.mockAnalisi; }
    const { data } = await http.get<AnalisiBolletta>(`/bollette/${id}`);
    return data;
  },
  async upload(contrattoId: string, file: File): Promise<AnalisiBolletta> {
    if (USE_MOCK) { await delay(800); return mock.mockAnalisi; }
    const form = new FormData();
    form.append('contrattoId', contrattoId);
    form.append('file', file);
    const { data } = await http.post<AnalisiBolletta>('/bollette/upload', form, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return data;
  },
};

// --- Dispositivi ---
export const dispositiviApi = {
  async lista(): Promise<Dispositivo[]> {
    if (USE_MOCK) { await delay(); return mock.mockDispositivi; }
    const { data } = await http.get<Dispositivo[]>('/dispositivi');
    return data;
  },
  async salva(payload: {
    nome: string; potenzaWatt: number; oreUtilizzoGiornaliere: number;
    giorniSettimana: number; fasciaPrevalente: FasciaOraria;
  }, id?: string): Promise<Dispositivo> {
    if (USE_MOCK) {
      await delay();
      const kwhG = (payload.potenzaWatt * payload.oreUtilizzoGiornaliere) / 1000;
      return {
        id: id ?? crypto.randomUUID(), ...payload,
        consumoGiornalieroKwh: +kwhG.toFixed(3),
        consumoMensileKwh: +(kwhG * (payload.giorniSettimana / 7) * 30).toFixed(2),
      };
    }
    const { data } = id
      ? await http.put<Dispositivo>(`/dispositivi/${id}`, payload)
      : await http.post<Dispositivo>('/dispositivi', payload);
    return data;
  },
  async elimina(id: string): Promise<void> {
    if (USE_MOCK) { await delay(); return; }
    await http.delete(`/dispositivi/${id}`);
  },
};

// --- Simulazione ---
export const simulazioneApi = {
  async dispositivi(contrattoId: string, dispositiviIds: string[]): Promise<SimulazioneDispositivi> {
    if (USE_MOCK) {
      await delay();
      const scelti = mock.mockDispositivi.filter((d) => dispositiviIds.length === 0 || dispositiviIds.includes(d.id));
      const mensile = scelti.reduce((s, d) => s + d.consumoMensileKwh, 0);
      const costo = mensile * 0.15 + 9.5;
      return {
        consumoTotaleGiornalieroKwh: +scelti.reduce((s, d) => s + d.consumoGiornalieroKwh, 0).toFixed(3),
        consumoTotaleMensileKwh: +mensile.toFixed(2),
        costoStimatoMensile: +costo.toFixed(2),
        costoStimatoAnnuo: +(costo * 12).toFixed(2),
        dettagli: scelti.map((d) => ({ nome: d.nome, consumoMensileKwh: d.consumoMensileKwh, costoMensile: +(d.consumoMensileKwh * 0.15).toFixed(2) })),
      };
    }
    const { data } = await http.post<SimulazioneDispositivi>('/simulazione/dispositivi', { contrattoId, dispositiviIds });
    return data;
  },
  async previsione(payload: {
    contrattoId: string; letturaAttuale: number; dataLetturaAttuale: string;
    letturaAttualeF1?: number; letturaAttualeF2?: number; letturaAttualeF3?: number;
  }): Promise<PrevisioneBolletta> {
    if (USE_MOCK) {
      await delay();
      const consumo = Math.max(0, payload.letturaAttuale - 15420) || payload.letturaAttuale;
      const proiettato = (consumo / 30) * 60;
      const materia = proiettato * 0.15;
      const quota = 9.5 * 2;
      const imposte = (materia + quota) * 0.1 + proiettato * 0.0227;
      return {
        consumoStimatoKwhOSm3: +proiettato.toFixed(2),
        giorniPeriodo: 60,
        mediaGiornaliera: +(consumo / 30).toFixed(3),
        costoMateriaPrima: +materia.toFixed(2),
        quotaFissa: +quota.toFixed(2),
        imposteStimate: +imposte.toFixed(2),
        importoStimatoTotale: +(materia + quota + imposte).toFixed(2),
        dataProssimaBollettaStimata: '2026-06-30T00:00:00Z',
        note: 'Stima mock basata sull\'ultima lettura di 15420 kWh.',
      };
    }
    const { data } = await http.post<PrevisioneBolletta>('/simulazione/previsione', payload);
    return data;
  },
};

export { TipoFornitura, TipoTariffa, FasciaOraria };
