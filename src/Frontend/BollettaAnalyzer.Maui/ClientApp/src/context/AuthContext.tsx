import { createContext, useContext, useEffect, useMemo, useState, ReactNode } from 'react';
import { authApi, tokenStore } from '../api/client';
import { Utente } from '../types';

interface AuthState {
  utente: Utente | null;
  loading: boolean;
  login: (email: string, password: string) => Promise<void>;
  register: (email: string, password: string, nome: string, cognome: string) => Promise<void>;
  logout: () => void;
  setUtente: (u: Utente) => void;
}

const AuthContext = createContext<AuthState | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [utente, setUtente] = useState<Utente | null>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const token = tokenStore.get();
    if (!token) { setLoading(false); return; }
    authApi.me()
      .then(setUtente)
      .catch(() => tokenStore.clear())
      .finally(() => setLoading(false));
  }, []);

  const login = async (email: string, password: string) => {
    const res = await authApi.login(email, password);
    tokenStore.set(res.token);
    setUtente(res.utente);
  };

  const register = async (email: string, password: string, nome: string, cognome: string) => {
    const res = await authApi.register(email, password, nome, cognome);
    tokenStore.set(res.token);
    setUtente(res.utente);
  };

  const logout = () => {
    tokenStore.clear();
    setUtente(null);
  };

  const value = useMemo<AuthState>(
    () => ({ utente, loading, login, register, logout, setUtente }),
    [utente, loading],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthState {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth deve essere usato dentro AuthProvider');
  return ctx;
}
