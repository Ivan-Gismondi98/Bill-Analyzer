import { NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

const tabs = [
  { to: '/dashboard', label: 'Dashboard', icon: '📊' },
  { to: '/simulatore', label: 'Simulatore', icon: '🔌' },
  { to: '/profilo', label: 'Profilo', icon: '👤' },
];

export default function Layout() {
  const { utente, logout } = useAuth();
  const navigate = useNavigate();

  const iniziali = `${utente?.nome?.[0] ?? ''}${utente?.cognome?.[0] ?? ''}`.toUpperCase();

  return (
    <div className="min-h-screen">
      <header className="sticky top-0 z-20 border-b border-white/50 bg-white/70 backdrop-blur-xl">
        <div className="mx-auto flex max-w-5xl items-center justify-between px-4 py-3">
          <div className="flex items-center gap-2.5">
            <span className="grid h-9 w-9 place-items-center rounded-2xl bg-brand-gradient text-white shadow-glow">⚡</span>
            <span className="text-lg font-extrabold tracking-tight text-gradient">Bolletta Analyzer</span>
          </div>
          <div className="flex items-center gap-3">
            <div className="hidden items-center gap-2 sm:flex">
              <span className="grid h-9 w-9 place-items-center rounded-full bg-brand-100 text-sm font-bold text-brand-700">{iniziali || '👤'}</span>
              <span className="text-sm font-medium text-slate-600">{utente?.nome}</span>
            </div>
            <button className="btn-ghost px-3 py-2 text-sm" onClick={() => { logout(); navigate('/'); }}>Esci</button>
          </div>
        </div>
      </header>

      <main className="mx-auto max-w-5xl px-4 pb-28 pt-6">
        <Outlet />
      </main>

      {/* Bottom navigation (mobile-first) con pill attiva */}
      <nav className="fixed inset-x-0 bottom-0 z-20 border-t border-white/50 bg-white/80 backdrop-blur-xl">
        <div className="mx-auto flex max-w-md items-center justify-around px-3 py-2">
          {tabs.map((t) => (
            <NavLink
              key={t.to}
              to={t.to}
              className={({ isActive }) =>
                `flex flex-1 flex-col items-center gap-0.5 rounded-2xl py-2 text-xs font-semibold transition-all ${
                  isActive ? 'bg-brand-gradient text-white shadow-soft' : 'text-slate-400 hover:text-brand-500'
                }`
              }
            >
              <span className="text-lg">{t.icon}</span>
              {t.label}
            </NavLink>
          ))}
        </div>
      </nav>
    </div>
  );
}
