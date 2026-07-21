import { Navigate, Route, Routes } from 'react-router-dom';
import { useAuth } from './context/AuthContext';
import Layout from './components/Layout';
import LandingPage from './pages/LandingPage';
import AuthPage from './pages/AuthPage';
import ProfiloPage from './pages/ProfiloPage';
import DashboardPage from './pages/DashboardPage';
import SimulatorePage from './pages/SimulatorePage';

function Protected({ children }: { children: JSX.Element }) {
  const { utente, loading } = useAuth();
  if (loading) return <div className="grid h-screen place-items-center text-slate-400">Caricamento…</div>;
  return utente ? children : <Navigate to="/auth" replace />;
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<LandingPage />} />
      <Route path="/auth" element={<AuthPage />} />

      <Route element={<Protected><Layout /></Protected>}>
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/profilo" element={<ProfiloPage />} />
        <Route path="/simulatore" element={<SimulatorePage />} />
      </Route>

      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  );
}
