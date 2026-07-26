import { useEffect, useMemo, useState } from 'react';
import { contrattiApi, dispositiviApi, lettureApi, simulazioneApi } from '../api/client';
import { Contratto, Dispositivo, FasciaOraria, LetturaContatore, PrevisioneBolletta, SimulazioneDispositivi, TipoFornitura } from '../types';

const fasce = [
  { v: FasciaOraria.F1, l: 'F1', desc: 'punta (lun-ven 8-19)' },
  { v: FasciaOraria.F2, l: 'F2', desc: 'intermedia (sere + sabato)' },
  { v: FasciaOraria.F3, l: 'F3', desc: 'fuori punta (notti e festivi)' },
];

const TUTTE_LE_FASCE = [FasciaOraria.F1, FasciaOraria.F2, FasciaOraria.F3];

/** Etichetta compatta delle fasce di un dispositivo ("Sempre attivo" se tutte e tre). */
const etichettaFasce = (fs: FasciaOraria[]) =>
  fs.length === 3 ? 'Sempre attivo' : fs.map((f) => FasciaOraria[f]).join(' + ');

export default function SimulatorePage() {
  const [contratti, setContratti] = useState<Contratto[]>([]);
  const [contrattoLuceId, setContrattoLuceId] = useState('');
  const [dispositivi, setDispositivi] = useState<Dispositivo[]>([]);
  const [selezionati, setSelezionati] = useState<Set<string>>(new Set());
  const [sim, setSim] = useState<SimulazioneDispositivi | null>(null);

  useEffect(() => {
    contrattiApi.lista().then((cs) => {
      // Nel simulatore contano solo i contratti ATTIVI (lo storico resta nel Profilo).
      const attivi = cs.filter((c) => c.attivo);
      setContratti(attivi);
      const luce = attivi.find((c) => c.tipoFornitura === TipoFornitura.Luce);
      if (luce) setContrattoLuceId(luce.id);
    });
    dispositiviApi.lista().then((ds) => {
      setDispositivi(ds);
      setSelezionati(new Set(ds.map((d) => d.id)));
    });
  }, []);

  const toggle = (id: string) => {
    setSelezionati((s) => {
      const n = new Set(s);
      n.has(id) ? n.delete(id) : n.add(id);
      return n;
    });
  };

  const simula = async () => {
    if (!contrattoLuceId) return;
    setSim(await simulazioneApi.dispositivi(contrattoLuceId, [...selezionati]));
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-black tracking-tight text-slate-900">Simulatore & Previsione</h1>
        <p className="text-sm text-slate-500">Stima consumi e prossima bolletta</p>
      </div>

      {/* --- Simulatore elettrodomestici --- */}
      <section className="space-y-3">
        <div className="flex items-center justify-between">
          <h2 className="font-semibold text-slate-700">🔌 Elettrodomestici</h2>
          <select className="input max-w-[12rem]" value={contrattoLuceId} onChange={(e) => setContrattoLuceId(e.target.value)}>
            {contratti.filter((c) => c.tipoFornitura === TipoFornitura.Luce).map((c) => (
              <option key={c.id} value={c.id}>{c.fornitore}</option>
            ))}
          </select>
        </div>

        <div className="grid gap-3 md:grid-cols-2">
          {dispositivi.map((d) => (
            <DispositivoRow
              key={d.id} d={d} selezionato={selezionati.has(d.id)} onToggle={() => toggle(d.id)}
              onDelete={async () => { await dispositiviApi.elimina(d.id); setDispositivi((l) => l.filter((x) => x.id !== d.id)); }}
            />
          ))}
          <NuovoDispositivo onCreated={(nd) => { setDispositivi((l) => [...l, nd]); setSelezionati((s) => new Set(s).add(nd.id)); }} />
        </div>

        <button className="btn-primary" onClick={simula}>Calcola impatto sui consumi</button>

        {sim && (
          <div className="grid gap-3 sm:grid-cols-4">
            <Stat label="Consumo giornaliero" value={`${sim.consumoTotaleGiornalieroKwh} kWh`} />
            <Stat label="Consumo mensile" value={`${sim.consumoTotaleMensileKwh} kWh`} />
            <Stat label="Costo mensile stimato" value={`€ ${sim.costoStimatoMensile}`} accent="text-brand-600" />
            <Stat label="Costo annuo stimato" value={`€ ${sim.costoStimatoAnnuo}`} />
          </div>
        )}
      </section>

      {/* --- Auto-lettura & previsione --- */}
      <PrevisioneSection contrattoId={contrattoLuceId} contratti={contratti} onContratto={setContrattoLuceId} />
    </div>
  );
}

function DispositivoRow({ d, selezionato, onToggle, onDelete }:
  { d: Dispositivo; selezionato: boolean; onToggle: () => void; onDelete: () => void }) {
  return (
    <div className={`card flex items-center gap-3 ${selezionato ? 'ring-2 ring-brand-200' : ''}`}>
      <input type="checkbox" checked={selezionato} onChange={onToggle} className="h-5 w-5 accent-brand-600" />
      <div className="flex-1">
        <div className="font-medium text-slate-800">{d.nome}</div>
        <div className="text-xs text-slate-500">
          {d.potenzaWatt} W · {d.oreUtilizzoGiornaliere} h/g · {d.giorniSettimana} gg/sett · {etichettaFasce(d.fasce)}
        </div>
        <div className="text-xs font-medium text-brand-600">≈ {d.consumoMensileKwh} kWh/mese</div>
      </div>
      <button className="text-slate-300 hover:text-red-500" onClick={onDelete} aria-label="Elimina">✕</button>
    </div>
  );
}

function NuovoDispositivo({ onCreated }: { onCreated: (d: Dispositivo) => void }) {
  const [open, setOpen] = useState(false);
  const [errore, setErrore] = useState('');
  const [f, setF] = useState({ nome: '', potenzaWatt: 1000, oreUtilizzoGiornaliere: 1, giorniSettimana: 7, fasce: [FasciaOraria.F1] as FasciaOraria[] });

  const toggleFascia = (v: FasciaOraria) =>
    setF((prev) => ({
      ...prev,
      fasce: prev.fasce.includes(v) ? prev.fasce.filter((x) => x !== v) : [...prev.fasce, v],
    }));

  const sempreAttivo = f.fasce.length === 3;

  const salva = async () => {
    if (!f.nome) return;
    if (f.fasce.length === 0) { setErrore('Seleziona almeno una fascia (o "Sempre attivo").'); return; }
    setErrore('');
    onCreated(await dispositiviApi.salva(f));
    setF({ nome: '', potenzaWatt: 1000, oreUtilizzoGiornaliere: 1, giorniSettimana: 7, fasce: [FasciaOraria.F1] });
    setOpen(false);
  };

  if (!open) return (
    <button className="card border-2 border-dashed border-slate-200 text-slate-500 hover:border-brand-300" onClick={() => setOpen(true)}>
      + Aggiungi dispositivo
    </button>
  );

  return (
    <div className="card space-y-2">
      <input className="input" placeholder="Nome (es. Asciugatrice)" value={f.nome} onChange={(e) => setF({ ...f, nome: e.target.value })} />
      <div className="grid grid-cols-3 gap-2">
        <label className="text-xs text-slate-500">Watt
          <input className="input" type="number" value={f.potenzaWatt} onChange={(e) => setF({ ...f, potenzaWatt: +e.target.value })} /></label>
        <label className="text-xs text-slate-500">Ore/giorno
          <input className="input" type="number" step="0.5" value={f.oreUtilizzoGiornaliere} onChange={(e) => setF({ ...f, oreUtilizzoGiornaliere: +e.target.value })} /></label>
        <label className="text-xs text-slate-500">Giorni/sett.
          <input className="input" type="number" min={1} max={7} value={f.giorniSettimana} onChange={(e) => setF({ ...f, giorniSettimana: +e.target.value })} /></label>
      </div>

      {/* Fasce di utilizzo: selezione multipla, "Sempre attivo" = tutte e tre. */}
      <div>
        <div className="mb-1 text-xs font-medium text-slate-500">Quando lo usi?</div>
        <div className="flex flex-wrap gap-2">
          <button type="button"
            className={`chip ${sempreAttivo ? 'bg-brand-gradient text-white shadow-soft' : 'bg-slate-100 text-slate-600'}`}
            onClick={() => setF({ ...f, fasce: sempreAttivo ? [FasciaOraria.F1] : [...TUTTE_LE_FASCE] })}>
            🔄 Sempre attivo
          </button>
          {fasce.map((x) => (
            <button key={x.v} type="button" title={x.desc}
              className={`chip ${f.fasce.includes(x.v) ? 'bg-brand-100 text-brand-700 ring-1 ring-brand-300' : 'bg-slate-100 text-slate-500'}`}
              onClick={() => toggleFascia(x.v)}>
              {x.l}
            </button>
          ))}
        </div>
        <p className="mt-1 text-[11px] text-slate-400">
          Il costo è calcolato con la media pesata dei prezzi delle fasce selezionate.
        </p>
      </div>

      {errore && <div className="rounded-lg bg-red-50 px-3 py-2 text-xs text-red-600">{errore}</div>}

      <div className="flex gap-2">
        <button className="btn-primary flex-1" onClick={salva}>Salva</button>
        <button className="btn-ghost" onClick={() => setOpen(false)}>Annulla</button>
      </div>
    </div>
  );
}

function PrevisioneSection({ contrattoId, contratti, onContratto }:
  { contrattoId: string; contratti: Contratto[]; onContratto: (id: string) => void }) {
  const [lettura, setLettura] = useState<number>(15680);
  const [data, setData] = useState<string>(new Date().toISOString().slice(0, 10));
  const [prev, setPrev] = useState<PrevisioneBolletta | null>(null);
  const [storico, setStorico] = useState<LetturaContatore[]>([]);
  const [busy, setBusy] = useState(false);

  const contratto = useMemo(() => contratti.find((c) => c.id === contrattoId), [contratti, contrattoId]);
  const unita = contratto?.tipoFornitura === TipoFornitura.Gas ? 'Sm³' : 'kWh';

  useEffect(() => {
    if (contrattoId) lettureApi.lista(contrattoId).then(setStorico);
  }, [contrattoId]);

  const calcola = async () => {
    if (!contrattoId) return;
    setBusy(true);
    try {
      // Salva l'auto-lettura (lo storico lato server tiene solo le ultime 3)…
      await lettureApi.crea({ contrattoId, dataLettura: new Date(data).toISOString(), valoreTotale: lettura });
      // …e calcola la previsione.
      setPrev(await simulazioneApi.previsione({
        contrattoId, letturaAttuale: lettura, dataLetturaAttuale: new Date(data).toISOString(),
      }));
      setStorico(await lettureApi.lista(contrattoId));
    } finally {
      setBusy(false);
    }
  };

  return (
    <section className="card space-y-4">
      <div>
        <h2 className="font-semibold text-slate-700">📷 Auto-lettura & previsione bolletta</h2>
        <p className="text-sm text-slate-500">Inserisci l'ultima lettura del contatore per stimare la prossima bolletta.</p>
      </div>

      <div className="grid gap-3 sm:grid-cols-3">
        <div>
          <label className="label">Contratto</label>
          <select className="input" value={contrattoId} onChange={(e) => onContratto(e.target.value)}>
            {contratti.map((c) => <option key={c.id} value={c.id}>{c.fornitore} — {c.tipoFornitura === TipoFornitura.Luce ? 'Luce' : 'Gas'}</option>)}
          </select>
        </div>
        <div>
          <label className="label">Lettura attuale ({unita})</label>
          <input className="input" type="number" value={lettura} onChange={(e) => setLettura(+e.target.value)} />
        </div>
        <div>
          <label className="label">Data lettura</label>
          <input className="input" type="date" value={data} onChange={(e) => setData(e.target.value)} />
        </div>
      </div>

      <button className="btn-primary" disabled={busy} onClick={calcola}>
        {busy ? 'Calcolo…' : 'Prevedi la prossima bolletta'}
      </button>

      {/* Storico ultime 3 auto-letture */}
      <div>
        <div className="mb-2 flex items-center gap-2 text-sm font-semibold text-slate-600">
          🕑 Ultime 3 auto-letture
          <span className="chip bg-slate-100 text-slate-500">max 3</span>
        </div>
        {storico.length === 0 ? (
          <p className="text-sm text-slate-400">Nessuna lettura registrata.</p>
        ) : (
          <ol className="space-y-2">
            {storico.slice(0, 3).map((l, i) => (
              <li key={l.id} className="flex items-center gap-3 rounded-2xl border border-slate-100 bg-white/70 px-3 py-2">
                <span className={`grid h-8 w-8 place-items-center rounded-xl text-sm font-bold text-white ${i === 0 ? 'bg-brand-gradient' : 'bg-slate-300'}`}>{i + 1}</span>
                <div className="flex-1">
                  <div className="text-sm font-semibold text-slate-700">{l.valoreTotale.toLocaleString('it-IT')} {unita}</div>
                  <div className="text-xs text-slate-400">
                    {new Date(l.dataLettura).toLocaleDateString('it-IT')}
                    {l.daBolletta && <span className="ml-1 text-emerald-600">· da bolletta</span>}
                    {l.note && <span className="ml-1">· {l.note}</span>}
                  </div>
                </div>
                {i === 0 && <span className="chip bg-brand-50 text-brand-600">più recente</span>}
              </li>
            ))}
          </ol>
        )}
      </div>

      {prev && (
        <div className="rounded-2xl bg-gradient-to-br from-brand-50 to-white p-5 ring-1 ring-brand-100">
          <div className="text-sm text-slate-500">Importo stimato prossima bolletta</div>
          <div className="text-4xl font-extrabold text-brand-700">€ {prev.importoStimatoTotale.toFixed(2)}</div>
          <div className="mt-3 grid gap-2 text-sm sm:grid-cols-2">
            <Row k="Consumo stimato periodo" v={`${prev.consumoStimatoKwhOSm3} ${unita} (${prev.giorniPeriodo} gg)`} />
            <Row k="Media giornaliera" v={`${prev.mediaGiornaliera} ${unita}/g`} />
            <Row k="Materia prima" v={`€ ${prev.costoMateriaPrima.toFixed(2)}`} />
            <Row k="Quota fissa" v={`€ ${prev.quotaFissa.toFixed(2)}`} />
            <Row k="Imposte e IVA" v={`€ ${prev.imposteStimate.toFixed(2)}`} />
            <Row k="Prossima bolletta attesa" v={new Date(prev.dataProssimaBollettaStimata).toLocaleDateString('it-IT')} />
          </div>
          <p className="mt-3 text-xs text-slate-400">{prev.note}</p>
        </div>
      )}
    </section>
  );
}

const Row = ({ k, v }: { k: string; v: string }) => (
  <div className="flex justify-between border-b border-slate-100 py-1">
    <span className="text-slate-500">{k}</span><span className="font-medium text-slate-700">{v}</span>
  </div>
);

function Stat({ label, value, accent }: { label: string; value: string; accent?: string }) {
  return (
    <div className="card">
      <div className="text-xs uppercase tracking-wide text-slate-400">{label}</div>
      <div className={`mt-1 text-xl font-bold ${accent ?? 'text-slate-800'}`}>{value}</div>
    </div>
  );
}
