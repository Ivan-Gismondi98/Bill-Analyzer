import { useEffect, useRef, useState } from 'react';
import {
  Bar, BarChart, Cell, Legend, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis,
} from 'recharts';
import { bolletteApi, contrattiApi } from '../api/client';
import { AnalisiBolletta, CategoriaCosto, Contratto, PrioritaSuggerimento, TipoFornitura } from '../types';

// Palette categorica validata (dataviz) per il breakdown costi.
const COLORI_COSTO = ['#2a78d6', '#eb6834', '#1baf7a', '#eda100'];
// Fasce: scala di stato (F1 cara → F3 conveniente). Identità ribadita dalle etichette F1/F2/F3.
const COLORI_FASCE = { F1: '#d03b3b', F2: '#fab219', F3: '#0ca30c' };

const etichettaCategoria = (c: CategoriaCosto) =>
  ['Materia energia', 'Trasporto e gestione', 'Oneri di sistema', 'Imposte e IVA'][c];

const prioritaStyle = (p: PrioritaSuggerimento) =>
  [
    'bg-slate-100 text-slate-600',
    'bg-amber-100 text-amber-700',
    'bg-red-100 text-red-700',
  ][p];

export default function DashboardPage() {
  const [contratti, setContratti] = useState<Contratto[]>([]);
  const [contrattoId, setContrattoId] = useState<string>('');
  const [analisi, setAnalisi] = useState<AnalisiBolletta | null>(null);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const fileRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    contrattiApi.lista().then((cs) => {
      // In dashboard si lavora sui contratti ATTIVI (lo storico vive nel Profilo).
      const attivi = cs.filter((c) => c.attivo);
      setContratti(attivi);
      const luce = attivi.find((c) => c.tipoFornitura === TipoFornitura.Luce) ?? attivi[0];
      if (luce) setContrattoId(luce.id);
    });
  }, []);

  useEffect(() => {
    // L'analisi mostrata segue il contratto selezionato: cambiando contratto
    // (es. da Luce a Gas) si carica l'ultima bolletta DI QUEL contratto.
    if (!contrattoId) return;
    let annullato = false;
    bolletteApi.lista(contrattoId).then(async (b) => {
      if (annullato) return;
      if (b.length) {
        const a = await bolletteApi.analisi(b[0].id);
        if (!annullato) setAnalisi(a);
      } else {
        setAnalisi(null);
      }
    });
    return () => { annullato = true; };
  }, [contrattoId]);

  const onUpload = async (file?: File) => {
    if (!file || !contrattoId) return;
    setUploading(true);
    setUploadError(null);
    try {
      setAnalisi(await bolletteApi.upload(contrattoId, file));
    } catch (err: any) {
      // 422 = documento illeggibile o dati non affidabili: il server spiega il perché.
      setUploadError(err?.response?.data?.message ?? 'Analisi non riuscita. Riprova con un altro file.');
    } finally {
      setUploading(false);
    }
  };

  const b = analisi?.bolletta;
  const contrattoSel = contratti.find((c) => c.id === contrattoId);
  const isGas = contrattoSel?.tipoFornitura === TipoFornitura.Gas;
  const datiFasce = b ? [
    { fascia: 'F1', kWh: b.consumoF1Kwh },
    { fascia: 'F2', kWh: b.consumoF2Kwh },
    { fascia: 'F3', kWh: b.consumoF3Kwh },
  ] : [];
  // Le fasce hanno senso solo per la luce (per il gas sono sempre 0).
  const mostraFasce = !isGas && datiFasce.some((d) => d.kWh > 0);

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div>
          <h1 className="text-3xl font-black tracking-tight text-slate-900">Dashboard</h1>
          <p className="text-sm text-slate-500">
            Analisi della tua ultima bolletta
            {b?.daOcr && b.confidenzaOcr != null && (
              <span className={`chip ml-2 ${b.confidenzaOcr >= 0.7 ? 'bg-emerald-100 text-emerald-700' : 'bg-amber-100 text-amber-700'}`}>
                OCR · confidenza {(b.confidenzaOcr * 100).toFixed(0)}% — verifica i valori
              </span>
            )}
          </p>
        </div>
        <select className="input max-w-xs" value={contrattoId} onChange={(e) => setContrattoId(e.target.value)}>
          {contratti.map((c) => (
            <option key={c.id} value={c.id}>{c.fornitore} — {c.tipoFornitura === TipoFornitura.Luce ? 'Luce' : 'Gas'}</option>
          ))}
        </select>
      </div>

      {/* Upload */}
      <section className="relative overflow-hidden rounded-3xl bg-brand-gradient p-6 text-white shadow-glow">
        <div className="relative z-10 flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-center">
          <div>
            <div className="text-lg font-bold">Carica una bolletta</div>
            <div className="text-sm text-white/80">PDF o foto. La analizziamo e ti spieghiamo ogni voce.</div>
            <div className="mt-1 text-xs text-white/70">🔒 Il file non viene conservato: teniamo solo i dati estratti, fino al prossimo upload.</div>
          </div>
          <input ref={fileRef} type="file" accept="application/pdf,image/*" className="hidden"
            onChange={(e) => onUpload(e.target.files?.[0])} />
          <button className="btn bg-white text-brand-700 shadow-soft hover:-translate-y-0.5" disabled={uploading} onClick={() => fileRef.current?.click()}>
            {uploading ? '⏳ Analisi in corso…' : '📤 Carica / Scatta foto'}
          </button>
        </div>
        <div className="pointer-events-none absolute -right-8 -top-10 h-40 w-40 rounded-full bg-white/10 blur-2xl" />
      </section>

      {uploadError && (
        <div className="rounded-2xl bg-red-50 px-4 py-3 text-sm text-red-700 ring-1 ring-red-100">
          ⚠️ {uploadError}
        </div>
      )}

      {!analisi ? (
        <div className="card text-center text-slate-400">Nessuna bolletta analizzata. Carica un documento per iniziare.</div>
      ) : (
        <>
          {/* Riepilogo */}
          <section className="grid gap-4 sm:grid-cols-3">
            <div className="relative overflow-hidden rounded-3xl bg-gradient-to-br from-indigo-500 to-violet-600 p-5 text-white shadow-glow">
              <div className="text-xs font-semibold uppercase tracking-wide text-white/70">Totale bolletta</div>
              <div className="mt-1 text-3xl font-black">€ {b!.importoTotale.toFixed(2)}</div>
              <div className="pointer-events-none absolute -right-6 -bottom-8 h-28 w-28 rounded-full bg-white/10 blur-xl" />
            </div>
            <Stat
              label="Consumo"
              value={isGas ? `${b!.consumoSm3} Sm³` : `${b!.consumoTotaleKwh} kWh`}
              icon={isGas ? '🔥' : '⚡'}
              grad={isGas ? 'from-cyan-400 to-sky-500' : 'from-amber-400 to-orange-500'}
            />
            <Stat label="Periodo" value={`${new Date(b!.periodoInizio).toLocaleDateString('it-IT')} → ${new Date(b!.periodoFine).toLocaleDateString('it-IT')}`} icon="📅" grad="from-cyan-400 to-blue-500" small />
          </section>

          <div className="grid gap-6 lg:grid-cols-2">
            {/* Breakdown costi */}
            <section className="card">
              <h2 className="mb-2 font-semibold text-slate-700">Composizione della spesa</h2>
              <ResponsiveContainer width="100%" height={260}>
                <PieChart>
                  <Pie
                    data={analisi.breakdownCosti.map((v) => ({ name: etichettaCategoria(v.categoria), value: v.importo }))}
                    dataKey="value" nameKey="name" cx="50%" cy="50%" outerRadius={90} label={(e) => `€${e.value}`}
                  >
                    {analisi.breakdownCosti.map((_, i) => <Cell key={i} fill={COLORI_COSTO[i % COLORI_COSTO.length]} />)}
                  </Pie>
                  <Tooltip formatter={(v: number) => `€ ${v.toFixed(2)}`} />
                  <Legend />
                </PieChart>
              </ResponsiveContainer>
            </section>

            {/* Fasce orarie (solo luce) */}
            {mostraFasce && (
            <section className="card">
              <h2 className="mb-2 font-semibold text-slate-700">Consumi per fascia oraria</h2>
              <ResponsiveContainer width="100%" height={260}>
                <BarChart data={datiFasce}>
                  <XAxis dataKey="fascia" />
                  <YAxis />
                  <Tooltip formatter={(v: number) => `${v} kWh`} />
                  <Bar dataKey="kWh" radius={[8, 8, 0, 0]}>
                    {datiFasce.map((d) => <Cell key={d.fascia} fill={(COLORI_FASCE as any)[d.fascia]} />)}
                  </Bar>
                </BarChart>
              </ResponsiveContainer>
              <p className="mt-2 text-center text-xs text-slate-400">
                F1 {analisi.ripartizioneFasce.percentualeF1}% · F2 {analisi.ripartizioneFasce.percentualeF2}% · F3 {analisi.ripartizioneFasce.percentualeF3}%
              </p>
            </section>
            )}
          </div>

          {/* Suggerimenti */}
          <section>
            <h2 className="mb-3 font-semibold text-slate-700">💡 Consigli per risparmiare</h2>
            <div className="space-y-3">
              {analisi.suggerimenti.map((s, i) => (
                <div key={i} className="card flex items-start gap-3">
                  <div className="grid h-10 w-10 shrink-0 place-items-center rounded-xl bg-brand-50 text-xl">💡</div>
                  <div className="flex-1">
                    <div className="flex items-center gap-2">
                      <span className="font-semibold text-slate-800">{s.titolo}</span>
                      <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${prioritaStyle(s.priorita)}`}>
                        {PrioritaSuggerimento[s.priorita]}
                      </span>
                    </div>
                    <p className="mt-1 text-sm text-slate-500">{s.descrizione}</p>
                    {s.risparmioStimatoAnnuo > 0 && (
                      <p className="mt-1 text-sm font-medium text-green-600">Risparmio stimato: € {s.risparmioStimatoAnnuo}/anno</p>
                    )}
                  </div>
                </div>
              ))}
            </div>
          </section>
        </>
      )}
    </div>
  );
}

function Stat({ label, value, icon, grad, small }: { label: string; value: string; icon?: string; grad?: string; small?: boolean }) {
  return (
    <div className="card flex items-center gap-3">
      {icon && <span className={`grid h-11 w-11 shrink-0 place-items-center rounded-2xl bg-gradient-to-br ${grad ?? 'from-slate-400 to-slate-500'} text-xl text-white shadow-soft`}>{icon}</span>}
      <div className="min-w-0">
        <div className="text-xs font-semibold uppercase tracking-wide text-slate-400">{label}</div>
        <div className={`mt-0.5 font-bold text-slate-800 ${small ? 'text-sm' : 'text-2xl'}`}>{value}</div>
      </div>
    </div>
  );
}
