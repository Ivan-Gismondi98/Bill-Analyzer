import { useEffect, useRef, useState } from 'react';
import { authApi, contrattiApi } from '../api/client';
import { Contratto, TipoFornitura, TipoTariffa } from '../types';
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

  const ricarica = () => contrattiApi.lista().then(setContratti);
  useEffect(() => { ricarica(); }, []);

  const attivi = contratti.filter((c) => c.attivo);
  const storico = contratti.filter((c) => !c.attivo);

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

      {/* Contratti attivi */}
      <section className="space-y-3">
        <h2 className="font-semibold text-slate-700">Contratti attuali</h2>
        <div className="grid gap-4 md:grid-cols-2">
          {attivi.map((c) => (
            <ContrattoCard key={c.id} contratto={c} onChanged={ricarica} />
          ))}
          <NuovoContrattoCard attivi={attivi} onCreated={ricarica} />
        </div>
      </section>

      {/* Storico: i contratti sostituiti restano confrontabili con l'attuale. */}
      {storico.length > 0 && (
        <section className="space-y-3">
          <div className="flex items-center gap-2">
            <h2 className="font-semibold text-slate-700">🗂️ Storico contratti</h2>
            <span className="chip bg-slate-100 text-slate-500">{storico.length}</span>
          </div>
          <div className="grid gap-4 md:grid-cols-2">
            {storico.map((c) => (
              <StoricoCard
                key={c.id}
                contratto={c}
                attuale={attivi.find((a) => a.tipoFornitura === c.tipoFornitura)}
                onChanged={ricarica}
              />
            ))}
          </div>
        </section>
      )}
    </div>
  );
}

/* ---------- Card contratto attivo (modifica + eliminazione) ---------- */

function ContrattoCard({ contratto, onChanged }: { contratto: Contratto; onChanged: () => void }) {
  const [c, setC] = useState(contratto);
  const [edit, setEdit] = useState(false);
  useEffect(() => setC(contratto), [contratto]);
  const isLuce = c.tipoFornitura === TipoFornitura.Luce;

  const salva = async () => {
    await contrattiApi.salva(c, c.id);
    setEdit(false);
    onChanged();
  };

  const elimina = async () => {
    if (!window.confirm(`Eliminare il contratto ${c.fornitore}? Verranno rimosse anche bollette e letture collegate.`)) return;
    await contrattiApi.elimina(c.id);
    onChanged();
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
        <div className="flex items-center gap-3">
          <button className="text-sm text-brand-600" onClick={() => setEdit((v) => !v)}>
            {edit ? 'Chiudi' : 'Modifica'}
          </button>
          <button className="text-sm text-red-400 hover:text-red-600" onClick={elimina} aria-label="Elimina contratto">✕</button>
        </div>
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
    </div>
  );
}

/* ---------- Card storico (confronto con l'attuale + eliminazione) ---------- */

function StoricoCard({ contratto, attuale, onChanged }:
  { contratto: Contratto; attuale?: Contratto; onChanged: () => void }) {
  const [confronta, setConfronta] = useState(false);
  const c = contratto;
  const isLuce = c.tipoFornitura === TipoFornitura.Luce;

  const elimina = async () => {
    if (!window.confirm(`Eliminare dal storico ${c.fornitore}? Verranno rimosse anche le bollette collegate.`)) return;
    await contrattiApi.elimina(c.id);
    onChanged();
  };

  // Delta prezzo: negativo = con l'attuale paghi MENO di prima (verde).
  const righe: [string, number, number][] = isLuce
    ? [
        ['€/kWh F1', c.prezzoKwhF1, attuale?.prezzoKwhF1 ?? 0],
        ['€/kWh F2', c.prezzoKwhF2, attuale?.prezzoKwhF2 ?? 0],
        ['€/kWh F3', c.prezzoKwhF3, attuale?.prezzoKwhF3 ?? 0],
        ['Quota fissa €/mese', c.quotaFissaMensile, attuale?.quotaFissaMensile ?? 0],
      ]
    : [
        ['€/Sm³', c.prezzoSm3, attuale?.prezzoSm3 ?? 0],
        ['Quota fissa €/mese', c.quotaFissaMensile, attuale?.quotaFissaMensile ?? 0],
      ];

  return (
    <div className="card border border-dashed border-slate-200 bg-slate-50/60">
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-2">
          <span className="grid h-9 w-9 place-items-center rounded-xl bg-slate-300 text-white">{isLuce ? '⚡' : '🔥'}</span>
          <div>
            <div className="flex items-center gap-2 font-semibold text-slate-600">
              {c.fornitore}
              <span className="chip bg-slate-200 text-slate-500">storico</span>
            </div>
            <div className="text-xs text-slate-400">{c.nomeOfferta}</div>
          </div>
        </div>
        <div className="flex items-center gap-3">
          {attuale && (
            <button className="text-sm text-brand-600" onClick={() => setConfronta((v) => !v)}>
              {confronta ? 'Chiudi' : 'Confronta'}
            </button>
          )}
          <button className="text-sm text-red-400 hover:text-red-600" onClick={elimina} aria-label="Elimina contratto storico">✕</button>
        </div>
      </div>

      {confronta && attuale && (
        <div className="mt-4 overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="text-left text-xs uppercase tracking-wide text-slate-400">
                <th className="py-1"> </th>
                <th className="py-1">Prima</th>
                <th className="py-1">Adesso ({attuale.fornitore})</th>
                <th className="py-1">Differenza</th>
              </tr>
            </thead>
            <tbody>
              {righe.map(([label, prima, adesso]) => {
                const delta = adesso - prima;
                return (
                  <tr key={label} className="border-t border-slate-100">
                    <td className="py-1.5 text-slate-500">{label}</td>
                    <td className="py-1.5 text-slate-600">{prima}</td>
                    <td className="py-1.5 font-medium text-slate-700">{adesso}</td>
                    <td className={`py-1.5 font-semibold ${delta < 0 ? 'text-green-600' : delta > 0 ? 'text-red-500' : 'text-slate-400'}`}>
                      {delta === 0 ? '=' : `${delta > 0 ? '+' : ''}${delta.toFixed(3).replace(/\.?0+$/, '')}`}
                      {delta < 0 && ' ↓'}
                      {delta > 0 && ' ↑'}
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
          <p className="mt-2 text-[11px] text-slate-400">Verde = con il contratto attuale paghi meno rispetto a questo storico.</p>
        </div>
      )}
    </div>
  );
}

/* ---------- Nuovo contratto: form locale, creato SOLO al salvataggio ---------- */

const nuovoVuoto = (tipo: TipoFornitura): Partial<Contratto> => ({
  tipoFornitura: tipo,
  fornitore: '',
  nomeOfferta: '',
  tipoTariffa: TipoTariffa.Monoraria,
  potenzaImpegnataKw: tipo === TipoFornitura.Luce ? 3 : 0,
  prezzoKwhMonorario: 0, prezzoKwhF1: 0, prezzoKwhF2: 0, prezzoKwhF3: 0,
  prezzoSm3: 0,
  quotaFissaMensile: 0,
});

function NuovoContrattoCard({ attivi, onCreated }: { attivi: Contratto[]; onCreated: () => void }) {
  const [form, setForm] = useState<Partial<Contratto> | null>(null);
  const [nota, setNota] = useState('');
  const [errore, setErrore] = useState('');
  const [busy, setBusy] = useState(false);
  const pdfRef = useRef<HTMLInputElement>(null);

  const isLuce = form?.tipoFornitura === TipoFornitura.Luce;
  const sostituira = form && attivi.find((a) => a.tipoFornitura === form.tipoFornitura);

  const apri = (tipo: TipoFornitura) => { setForm(nuovoVuoto(tipo)); setNota(''); setErrore(''); };
  const annulla = () => { setForm(null); setNota(''); setErrore(''); };

  /** PDF → dati estratti per precompilare il form. Il file NON viene salvato da nessuna parte. */
  const compilaDaPdf = async (file?: File) => {
    if (!file || !form) return;
    setBusy(true);
    setErrore('');
    try {
      const e = await contrattiApi.analizza(file);
      setForm((prev) => prev && ({
        ...prev,
        ...(e.tipoFornitura != null ? { tipoFornitura: e.tipoFornitura } : {}),
        ...(e.fornitore ? { fornitore: e.fornitore } : {}),
        ...(e.codicePod ? { codicePod: e.codicePod } : {}),
        ...(e.codicePdr ? { codicePdr: e.codicePdr } : {}),
        ...(e.nomeOfferta ? { nomeOfferta: e.nomeOfferta } : {}),
        ...(e.tipoTariffa != null ? { tipoTariffa: e.tipoTariffa } : {}),
        ...(e.potenzaImpegnataKw != null ? { potenzaImpegnataKw: e.potenzaImpegnataKw } : {}),
        ...(e.prezzoKwhMonorario != null ? { prezzoKwhMonorario: e.prezzoKwhMonorario } : {}),
        ...(e.prezzoKwhF1 != null ? { prezzoKwhF1: e.prezzoKwhF1 } : {}),
        ...(e.prezzoKwhF2 != null ? { prezzoKwhF2: e.prezzoKwhF2 } : {}),
        ...(e.prezzoKwhF3 != null ? { prezzoKwhF3: e.prezzoKwhF3 } : {}),
        ...(e.prezzoSm3 != null ? { prezzoSm3: e.prezzoSm3 } : {}),
        ...(e.quotaFissaMensile != null ? { quotaFissaMensile: e.quotaFissaMensile } : {}),
      }));
      setNota(`Dati estratti dal PDF (confidenza ${(e.confidenza * 100).toFixed(0)}%) — controlla e correggi prima di salvare. Il file non viene conservato.`);
    } catch (err: any) {
      setErrore(err?.response?.data?.message ?? 'Impossibile leggere il PDF: compila i campi a mano.');
    } finally {
      setBusy(false);
      if (pdfRef.current) pdfRef.current.value = '';
    }
  };

  const salva = async () => {
    if (!form) return;
    if (!form.fornitore?.trim()) { setErrore('Indica il fornitore.'); return; }
    setErrore('');
    setBusy(true);
    try {
      await contrattiApi.salva(form);
      setForm(null);
      setNota('');
      onCreated();
    } catch (err: any) {
      setErrore(err?.response?.data?.message ?? 'Salvataggio non riuscito.');
    } finally {
      setBusy(false);
    }
  };

  if (!form) {
    return (
      <div className="card border-2 border-dashed border-slate-200 bg-slate-50/50">
        <div className="mb-2 font-semibold text-slate-700">Aggiungi contratto</div>
        <p className="mb-4 text-sm text-slate-500">
          Il nuovo contratto diventa quello attuale; il precedente passa nello storico.
        </p>
        <div className="flex gap-2">
          <button className="btn-ghost flex-1" onClick={() => apri(TipoFornitura.Luce)}>⚡ Luce</button>
          <button className="btn-ghost flex-1" onClick={() => apri(TipoFornitura.Gas)}>🔥 Gas</button>
        </div>
      </div>
    );
  }

  return (
    <div className="card space-y-3 ring-2 ring-brand-200">
      <div className="flex items-center justify-between">
        <div className="font-semibold text-slate-800">Nuovo contratto {isLuce ? '⚡ Luce' : '🔥 Gas'}</div>
        <button className="text-sm text-slate-400 hover:text-slate-600" onClick={annulla}>Annulla</button>
      </div>

      {/* Compilazione automatica dal PDF: si estraggono solo i dati, come per le bollette. */}
      <input ref={pdfRef} type="file" accept="application/pdf" className="hidden" onChange={(e) => compilaDaPdf(e.target.files?.[0])} />
      <button className="btn-soft w-full text-sm" disabled={busy} onClick={() => pdfRef.current?.click()}>
        {busy ? 'Analisi…' : '📄 Compila automaticamente dal PDF del contratto'}
      </button>
      {nota && <div className="rounded-lg bg-emerald-50 px-3 py-2 text-xs text-emerald-700">{nota}</div>}

      <div className="grid gap-3 sm:grid-cols-2">
        <Field label="Fornitore" value={form.fornitore ?? ''} onChange={(v) => setForm({ ...form, fornitore: v })} />
        <Field label="Offerta" value={form.nomeOfferta ?? ''} onChange={(v) => setForm({ ...form, nomeOfferta: v })} />
        {isLuce ? (
          <>
            <Field label="POD (opzionale)" value={form.codicePod ?? ''} onChange={(v) => setForm({ ...form, codicePod: v })} />
            <NumField label="Potenza kW" value={form.potenzaImpegnataKw ?? 0} onChange={(v) => setForm({ ...form, potenzaImpegnataKw: v })} />
            <div className="sm:col-span-2">
              <label className="label">Tariffa</label>
              <select className="input" value={form.tipoTariffa}
                onChange={(e) => setForm({ ...form, tipoTariffa: +e.target.value })}>
                <option value={TipoTariffa.Monoraria}>Monoraria</option>
                <option value={TipoTariffa.Bioraria}>Bioraria</option>
                <option value={TipoTariffa.Multioraria}>Multioraria</option>
              </select>
            </div>
            {form.tipoTariffa === TipoTariffa.Monoraria ? (
              <NumField label="€/kWh" value={form.prezzoKwhMonorario ?? 0} onChange={(v) => setForm({ ...form, prezzoKwhMonorario: v })} />
            ) : (
              <>
                <NumField label="€/kWh F1" value={form.prezzoKwhF1 ?? 0} onChange={(v) => setForm({ ...form, prezzoKwhF1: v })} />
                <NumField label="€/kWh F2" value={form.prezzoKwhF2 ?? 0} onChange={(v) => setForm({ ...form, prezzoKwhF2: v })} />
                <NumField label="€/kWh F3" value={form.prezzoKwhF3 ?? 0} onChange={(v) => setForm({ ...form, prezzoKwhF3: v })} />
              </>
            )}
          </>
        ) : (
          <>
            <Field label="PDR (opzionale)" value={form.codicePdr ?? ''} onChange={(v) => setForm({ ...form, codicePdr: v })} />
            <NumField label="€/Sm³" value={form.prezzoSm3 ?? 0} onChange={(v) => setForm({ ...form, prezzoSm3: v })} />
          </>
        )}
        <NumField label="Quota fissa €/mese" value={form.quotaFissaMensile ?? 0} onChange={(v) => setForm({ ...form, quotaFissaMensile: v })} />
      </div>

      {sostituira && (
        <div className="rounded-lg bg-amber-50 px-3 py-2 text-xs text-amber-700">
          ℹ️ Il contratto attuale <b>{sostituira.fornitore}</b> passerà nello storico (potrai confrontarlo ed eliminarlo).
        </div>
      )}
      {errore && <div className="rounded-lg bg-red-50 px-3 py-2 text-xs text-red-600">{errore}</div>}

      <button className="btn-primary w-full" disabled={busy} onClick={salva}>
        {busy ? 'Salvataggio…' : 'Salva contratto'}
      </button>
    </div>
  );
}

/* ---------- Piccoli componenti ---------- */

const Info = ({ label, value }: { label: string; value: string }) => (
  <div><dt className="text-slate-400">{label}</dt><dd className="font-medium text-slate-700">{value}</dd></div>
);
const Field = ({ label, value, onChange }: { label: string; value: string; onChange: (v: string) => void }) => (
  <div><label className="label">{label}</label><input className="input" value={value} onChange={(e) => onChange(e.target.value)} /></div>
);
const NumField = ({ label, value, onChange }: { label: string; value: number; onChange: (v: number) => void }) => (
  <div><label className="label">{label}</label><input className="input" type="number" step="0.001" value={value} onChange={(e) => onChange(parseFloat(e.target.value) || 0)} /></div>
);
