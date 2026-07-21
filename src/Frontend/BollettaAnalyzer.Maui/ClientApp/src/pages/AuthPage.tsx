import { FormEvent, useState } from 'react';
import { useNavigate, useSearchParams } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';

export default function AuthPage() {
  const [params] = useSearchParams();
  const [isRegister, setIsRegister] = useState(params.get('mode') === 'register');
  const [email, setEmail] = useState('demo@bolletta.app');
  const [password, setPassword] = useState('Password1!');
  const [nome, setNome] = useState('');
  const [cognome, setCognome] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const { login, register } = useAuth();
  const navigate = useNavigate();

  const submit = async (e: FormEvent) => {
    e.preventDefault();
    setError(null);
    setBusy(true);
    try {
      if (isRegister) await register(email, password, nome, cognome);
      else await login(email, password);
      navigate('/dashboard');
    } catch (err: any) {
      setError(err?.response?.data?.message ?? 'Operazione non riuscita. Riprova.');
    } finally {
      setBusy(false);
    }
  };

  return (
    <div className="grid min-h-screen place-items-center bg-hero-gradient px-4">
      <div className="w-full max-w-md animate-scale-in">
        <button className="mb-4 text-sm font-medium text-slate-500 hover:text-brand-600" onClick={() => navigate('/')}>‹ Torna alla home</button>
        <div className="card-glass">
          <div className="mb-4 flex items-center gap-3">
            <span className="grid h-11 w-11 place-items-center rounded-2xl bg-brand-gradient text-xl text-white shadow-glow">⚡</span>
            <div>
              <h1 className="text-2xl font-black text-slate-800">{isRegister ? 'Crea il tuo account' : 'Bentornato'}</h1>
              <p className="text-sm text-slate-500">
                {isRegister ? 'Inizia ad analizzare le tue bollette.' : 'Accedi per continuare.'}
              </p>
            </div>
          </div>

          <form className="mt-6 space-y-4" onSubmit={submit}>
            {isRegister && (
              <div className="grid grid-cols-2 gap-3">
                <div>
                  <label className="label">Nome</label>
                  <input className="input" value={nome} onChange={(e) => setNome(e.target.value)} required />
                </div>
                <div>
                  <label className="label">Cognome</label>
                  <input className="input" value={cognome} onChange={(e) => setCognome(e.target.value)} required />
                </div>
              </div>
            )}
            <div>
              <label className="label">Email</label>
              <input className="input" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />
            </div>
            <div>
              <label className="label">Password</label>
              <input className="input" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required minLength={8} />
            </div>

            {error && <div className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-600">{error}</div>}

            <button className="btn-primary w-full" disabled={busy}>
              {busy ? 'Attendere…' : isRegister ? 'Registrati' : 'Accedi'}
            </button>
          </form>

          <div className="mt-4 text-center text-sm text-slate-500">
            {isRegister ? 'Hai già un account?' : 'Non hai un account?'}{' '}
            <button className="font-semibold text-brand-600" onClick={() => setIsRegister((v) => !v)}>
              {isRegister ? 'Accedi' : 'Registrati'}
            </button>
          </div>
        </div>
        <p className="mt-3 text-center text-xs text-slate-400">Demo: demo@bolletta.app / Password1!</p>
      </div>
    </div>
  );
}
