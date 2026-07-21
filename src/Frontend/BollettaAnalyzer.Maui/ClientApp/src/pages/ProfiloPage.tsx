import { useEffect, useRef, useState } from 'react';
import { authApi, contrattiApi } from '../api/client';
import { Contratto, DocumentoContrattoInfo, TipoFornitura, TipoTariffa } from '../types';
import { useAuth } from '../context/AuthContext';

export default function ProfiloPage() {
  const { utente, setUtente } = useAuth();
  const [contratti, setContratti] = useState<Contratto[]>([]);
  const [saved, setSaved] = useState('');
  const [form, setForm] = useState({
    nome: utente?.nome ?? '', cognome: utente?.cognome ?? '',
    telefono: utente?.telefono ?? '', indirizzo: utente?.indirizzo ?? '',
    citta: utente?.citta ?? '', cap: utente?.cap ?? '',
  });

  useEffect(() => { contrattiApi.lista().then(setContratti); }, []);

  const salvaProfilo = async () => {
    const u = await authApi.updateProfilo(form);
    setUtente(u);
    setSaved('Profilo aggiornato ✓');
    setTimeout(() => setSaved(''), 2500);
  };

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-3xl font-black tracking-tight text-slate-900">Profilo & Contratti</h1>
        <p className="text-sm text-slate-500">Gestisci i tuoi dati e le forniture luce/gas</p>
      </div>

      {/* Anagrafica */}
      <section className="card">
        <h2 className="mb-4 font-semibold text-slate-700">Dati anagrafici</h2>
        <div className="grid gap-3 sm:grid-cols-2">
          {([
            ['nome', 'Nome'], ['cognome', 'Cognome'], ['telefono', 'Telefono'],
            ['indirizzo', 'Indirizzo'], ['citta', 'Città'], ['cap', 'CAP'],
          ] as const).map(([key, lbl]) => (
            <div key={key}>
              <label className="label">{lbl}</label>
              <input
                className="input"
                value={(form as any)[key] ?? ''}
                onChange={(e) => setForm({ ...form, [key]: e.target.value })}
              />
            </div>
          ))}
        </div>
        <div className="mt-4 flex items-center gap-3">
          <button className="btn-primary" onClick={salvaProfilo}>Salva profilo</button>
          {saved && <span className="text-sm text-green-600">{saved}</span>}
        </div>
      </section>

      {/* Contratti */}
      <section className="space-y-3">
        <div className="flex items-center justify-between">
          <h2 className="font-semibold text-slate-700">Contratti Luce & Gas</h2>
        </div>
        <div className="grid gap-4 md:grid-cols-2">
          {contratti.map((c) => (
            <ContrattoCard key={c.id} contratto={c} onSaved={(nc) =>
              setContratti((list) => list.map((x) => (x.id === nc.id ? nc : x)))} />
          ))}
          <NuovoContrattoCard onCreated={(nc) => setContratti((l) => [...l, nc])} />
        </div>
      </section>
    </div>
  );
}

function ContrattoCard({ contratto, onSaved }: { contratto: Contratto; onSaved: (c: Contratto) => void }) {
  const [c, setC] = useState(contratto);
  const [edit, setEdit] = useState(false);
  const isLuce = c.tipoFornitura === TipoFornitura.Luce;

  const salva = async () => {
    const nc = await contrattiApi.salva(c, c.id);
    onSaved(nc);
    setEdit(false);
  };

  return (
    <div className="card">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span className={`grid h-9 w-9 place-items-center rounded-xl text-white ${isLuce ? 'bg-energy' : 'bg-gas'}`}>
            {isLuce ? '⚡' : '🔥'}
          </span>
          <div>
            <div className="font-semibold text-slate-800">{c.fornitore}</div>
            <div className="text-xs text-slate-500">{c.nomeOfferta}</div>
          </div>
        </div>
        <button className="text-sm text-brand-600" onClick={() => setEdit((v) => !v)}>
          {edit ? 'Chiudi' : 'Modifica'}
        </button>
      </div>

      {!edit ? (
        <dl className="mt-4 grid grid-cols-2 gap-2 text-sm">
          {isLuce ? (
            <>
              <Info label="Tariffa" value={TipoTariffa[c.tipoTariffa]} />
              <Info label="Potenza" value={`${c.potenzaImpegnataKw} kW`} />
              <Info label="F1 / F2 / F3" value={`${c.prezzoKwhF1} / ${c.prezzoKwhF2} / ${c.prezzoKwhF3} €/kWh`} />
            </>
          ) : (
            <Info label="Prezzo gas" value={`${c.prezzoSm3} €/Sm³`} />
          )}
          <Info label="Quota fissa" value={`${c.quotaFissaMensile} €/mese`} />
        </dl>
      ) : (
        <div className="mt-4 grid gap-3 sm:grid-cols-2">
          <Field label="Fornitore" value={c.fornitore} onChange={(v) => setC({ ...c, fornitore: v })} />
          <Field label="Offerta" value={c.nomeOfferta} onChange={(v) => setC({ ...c, nomeOfferta: v })} />
          {isLuce ? (
            <>
              <NumField label="Potenza kW" value={c.potenzaImpegnataKw} onChange={(v) => setC({ ...c, potenzaImpegnataKw: v })} />
              <NumField label="€/kWh F1" value={c.prezzoKwhF1} onChange={(v) => setC({ ...c, prezzoKwhF1: v })} />
              <NumField label="€/kWh F2" value={c.prezzoKwhF2} onChange={(v) => setC({ ...c, prezzoKwhF2: v })} />
              <NumField label="€/kWh F3" value={c.prezzoKwhF3} onChange={(v) => setC({ ...c, prezzoKwhF3: v })} />
            </>
          ) : (
            <NumField label="€/Sm³" value={c.prezzoSm3} onChange={(v) => setC({ ...c, prezzoSm3: v })} />
          )}
          <NumField label="Quota fissa €/mese" value={c.quotaFissaMensile} onChange={(v) => setC({ ...c, quotaFissaMensile: v })} />
          <div className="sm:col-span-2">
            <button className="btn-primary w-full" onClick={salva}>Salva contratto</button>
          </div>
        </div>
      )}

      <DocumentoContrattoBox contrattoId={c.id} />
    </div>
  );
}

/** Upload/download del PDF di contratto, conservato cifrato lato server. */
function DocumentoContrattoBox({ contrattoId }: { contrattoId: string }) {
  const [doc, setDoc] = useState<DocumentoContrattoInfo | null>(null);
  const [busy, setBusy] = useState(false);
  const fileRef = useRef<HTMLInputElement>(null);

  useEffect(() => { contrattiApi.documentoInfo(contrattoId).then(setDoc); }, [contrattoId]);

  const carica = async (file?: File) => {
    if (!file) return;
    setBusy(true);
    try { setDoc(await contrattiApi.caricaDocumento(contrattoId, file)); }
    finally { setBusy(false); }
  };

  const elimina = async () => {
    await contrattiApi.eliminaDocumento(contrattoId);
    setDoc(null);
  };

  const scarica = async () => {
    // Scarica il blob autenticato (Bearer) e lo apre/salva via object URL.
    const blob = await contrattiApi.scaricaDocumento(contrattoId);
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = doc?.nomeFile ?? 'contratto.pdf';
    a.click();
    URL.revokeObjectURL(url);
  };

  return (
    <div className="mt-4 rounded-2xl border border-slate-100 bg-slate-50/70 p-3">
      <div className="mb-2 flex items-center gap-2 text-sm font-semibold text-slate-600">
        <span>📄 Documento contratto</span>
        <span className="chip bg-emerald-100 text-emerald-700">🔒 cifrato</span>
      </div>
      <input ref={fileRef} type="file" accept="application/pdf" className="hidden" onChange={(e) => carica(e.target.files?.[0])} />
      {doc ? (
        <div className="flex flex-wrap items-center justify-between gap-2">
          <div className="min-w-0 text-sm">
            <div className="truncate font-medium text-slate-700">{doc.nomeFile}</div>
            <div className="text-xs text-slate-400">{(doc.dimensioneByte / 1024).toFixed(0)} KB · caricato il {new Date(doc.caricatoIl).toLocaleDateString('it-IT')}</div>
          </div>
          <div className="flex gap-2">
            <button className="btn-soft px-3 py-1.5 text-xs" onClick={scarica}>⬇︎ Scarica</button>
            <button className="btn-ghost px-3 py-1.5 text-xs" disabled={busy} onClick={() => fileRef.current?.click()}>Sostituisci</button>
            <button className="btn-ghost px-3 py-1.5 text-xs text-red-500" onClick={elimina}>Elimina</button>
          </div>
        </div>
      ) : (
        <button className="btn-ghost w-full text-sm" disabled={busy} onClick={() => fileRef.current?.click()}>
          {busy ? 'Caricamento…' : '＋ Carica PDF del contratto'}
        </button>
      )}
    </div>
  );
}

function NuovoContrattoCard({ onCreated }: { onCreated: (c: Contratto) => void }) {
  const crea = async (tipo: TipoFornitura) => {
    const nc = await contrattiApi.salva({
      tipoFornitura: tipo,
      fornitore: 'Nuovo fornitore',
      nomeOfferta: 'Nuova offerta',
      tipoTariffa: tipo === TipoFornitura.Luce ? TipoTariffa.Monoraria : TipoTariffa.Monoraria,
      potenzaImpegnataKw: tipo === TipoFornitura.Luce ? 3 : 0,
      prezzoKwhMonorario: 0.14, prezzoKwhF1: 0, prezzoKwhF2: 0, prezzoKwhF3: 0,
      prezzoSm3: tipo === TipoFornitura.Gas ? 0.5 : 0,
      quotaFissaMensile: 8,
    });
    onCreated(nc);
  };

  return (
    <div className="card border-2 border-dashed border-slate-200 bg-slate-50/50">
      <div className="mb-2 font-semibold text-slate-700">Aggiungi contratto</div>
      <p className="mb-4 text-sm text-slate-500">Configura una nuova fornitura per analizzare le sue bollette.</p>
      <div className="flex gap-2">
        <button className="btn-ghost flex-1" onClick={() => crea(TipoFornitura.Luce)}>⚡ Luce</button>
        <button className="btn-ghost flex-1" onClick={() => crea(TipoFornitura.Gas)}>🔥 Gas</button>
      </div>
    </div>
  );
}

const Info = ({ label, value }: { label: string; value: string }) => (
  <div><dt className="text-slate-400">{label}</dt><dd className="font-medium text-slate-700">{value}</dd></div>
);
const Field = ({ label, value, onChange }: { label: string; value: string; onChange: (v: string) => void }) => (
  <div><label className="label">{label}</label><input className="input" value={value} onChange={(e) => onChange(e.target.value)} /></div>
);
const NumField = ({ label, value, onChange }: { label: string; value: number; onChange: (v: number) => void }) => (
  <div><label className="label">{label}</label><input className="input" type="number" step="0.001" value={value} onChange={(e) => onChange(parseFloat(e.target.value) || 0)} /></div>
);
