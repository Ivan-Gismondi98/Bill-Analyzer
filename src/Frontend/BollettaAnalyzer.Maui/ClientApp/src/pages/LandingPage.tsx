import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

const slides = [
  { icon: '🧾', titolo: 'Leggi la bolletta senza sforzo', testo: 'Carica il PDF o scatta una foto: ti spieghiamo ogni voce di spesa in parole semplici.', from: 'from-indigo-500', to: 'to-violet-500' },
  { icon: '📈', titolo: 'Analizza i tuoi consumi', testo: 'Scopri in quali fasce orarie (F1/F2/F3) consumi di più con grafici chiari e immediati.', from: 'from-cyan-500', to: 'to-blue-500' },
  { icon: '💡', titolo: 'Consigli su misura', testo: 'Ricevi suggerimenti personalizzati per risparmiare, calcolati sui tuoi dati reali.', from: 'from-amber-500', to: 'to-orange-500' },
  { icon: '🔮', titolo: 'Simula e prevedi', testo: "Stima la prossima bolletta dall'auto-lettura e simula l'impatto degli elettrodomestici.", from: 'from-emerald-500', to: 'to-teal-500' },
];

export default function LandingPage() {
  const navigate = useNavigate();
  const [idx, setIdx] = useState(0);
  const s = slides[idx];

  return (
    <div className="min-h-screen bg-hero-gradient">
      <div className="mx-auto max-w-3xl px-5 py-12">
        {/* Hero */}
        <section className="text-center animate-fade-up">
          <span className="mx-auto mb-5 grid h-20 w-20 animate-float place-items-center rounded-[1.75rem] bg-brand-gradient text-4xl text-white shadow-glow">⚡</span>
          <h1 className="text-4xl font-black leading-tight tracking-tight text-slate-900 sm:text-5xl">
            Capisci la tua bolletta.<br />
            <span className="text-gradient">Risparmia davvero.</span>
          </h1>
          <p className="mx-auto mt-4 max-w-xl text-lg text-slate-500">
            Bolletta Analyzer traduce luce e gas in numeri chiari, analizza i tuoi consumi
            e ti dice come spendere meno — su mobile e desktop.
          </p>
          <div className="mt-7 flex justify-center gap-3">
            <button className="btn-primary px-6 py-3 text-base" onClick={() => navigate('/auth?mode=register')}>Registrati gratis</button>
            <button className="btn-ghost px-6 py-3 text-base" onClick={() => navigate('/auth')}>Accedi</button>
          </div>
        </section>

        {/* Onboarding carousel */}
        <section className="mt-14 animate-scale-in">
          <div className="card-glass overflow-hidden">
            <div className="text-center">
              <div className={`mx-auto grid h-20 w-20 place-items-center rounded-3xl bg-gradient-to-br ${s.from} ${s.to} text-4xl text-white shadow-soft`}>
                {s.icon}
              </div>
              <h2 className="mt-5 text-2xl font-bold text-slate-800">{s.titolo}</h2>
              <p className="mx-auto mt-2 max-w-md text-slate-500">{s.testo}</p>
            </div>
            <div className="mt-7 flex items-center justify-between">
              <button className="btn-ghost text-sm disabled:opacity-30" disabled={idx === 0} onClick={() => setIdx((i) => Math.max(0, i - 1))}>‹ Indietro</button>
              <div className="flex gap-1.5">
                {slides.map((_, i) => (
                  <button key={i} onClick={() => setIdx(i)}
                    className={`h-2 rounded-full transition-all ${i === idx ? 'w-7 bg-brand-gradient' : 'w-2 bg-slate-300'}`}
                    aria-label={`Slide ${i + 1}`} />
                ))}
              </div>
              {idx < slides.length - 1
                ? <button className="btn-soft text-sm" onClick={() => setIdx((i) => i + 1)}>Avanti ›</button>
                : <button className="btn-primary text-sm" onClick={() => navigate('/auth?mode=register')}>Inizia →</button>}
            </div>
          </div>
        </section>

        {/* Vantaggi */}
        <section className="mt-8 grid gap-4 sm:grid-cols-3">
          {[
            ['🔍', 'Trasparenza', 'Ogni euro spiegato: materia prima, trasporto, oneri, imposte.', 'from-indigo-500 to-violet-500'],
            ['⏱️', 'Fasce orarie', 'Sposta i consumi in F3 e paga meno.', 'from-cyan-500 to-blue-500'],
            ['🔒', 'Dati al sicuro', 'Il contratto è conservato cifrato; le bollette non ingombrano.', 'from-emerald-500 to-teal-500'],
          ].map(([icon, t, d, grad], i) => (
            <div key={t} className="card animate-fade-up" style={{ animationDelay: `${i * 80}ms` }}>
              <div className={`grid h-11 w-11 place-items-center rounded-2xl bg-gradient-to-br ${grad} text-xl text-white shadow-soft`}>{icon}</div>
              <div className="mt-3 font-bold text-slate-800">{t}</div>
              <div className="mt-1 text-sm text-slate-500">{d}</div>
            </div>
          ))}
        </section>
      </div>
    </div>
  );
}
