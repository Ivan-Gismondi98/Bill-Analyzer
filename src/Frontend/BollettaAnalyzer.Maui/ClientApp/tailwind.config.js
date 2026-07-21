/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{ts,tsx}'],
  theme: {
    extend: {
      colors: {
        // Brand: indaco → violetto, moderno e vivace
        brand: {
          50: '#eef2ff', 100: '#e0e7ff', 200: '#c7d2fe', 300: '#a5b4fc',
          400: '#818cf8', 500: '#6366f1', 600: '#4f46e5', 700: '#4338ca',
          800: '#3730a3', 900: '#312e81',
        },
        // Accenti per fornitura
        energy: { DEFAULT: '#f59e0b', soft: '#fef3c7', deep: '#b45309' },
        gas: { DEFAULT: '#06b6d4', soft: '#cffafe', deep: '#0e7490' },
        // Slot categorici validati (dataviz) per i grafici
        viz: { 1: '#2a78d6', 2: '#eb6834', 3: '#1baf7a', 4: '#eda100' },
      },
      fontFamily: {
        sans: ['system-ui', '-apple-system', 'Segoe UI', 'Roboto', 'sans-serif'],
      },
      boxShadow: {
        soft: '0 4px 24px -8px rgba(79,70,229,0.18)',
        glow: '0 8px 40px -8px rgba(99,102,241,0.45)',
        card: '0 1px 3px rgba(15,23,42,0.06), 0 8px 24px -12px rgba(15,23,42,0.12)',
      },
      backgroundImage: {
        'brand-gradient': 'linear-gradient(135deg,#6366f1 0%,#8b5cf6 50%,#06b6d4 100%)',
        'hero-gradient': 'radial-gradient(1200px 600px at 20% -10%,#e0e7ff 0%,transparent 55%),radial-gradient(1000px 500px at 100% 0%,#cffafe 0%,transparent 50%)',
      },
      keyframes: {
        'fade-up': { '0%': { opacity: '0', transform: 'translateY(12px)' }, '100%': { opacity: '1', transform: 'translateY(0)' } },
        'scale-in': { '0%': { opacity: '0', transform: 'scale(.96)' }, '100%': { opacity: '1', transform: 'scale(1)' } },
        float: { '0%,100%': { transform: 'translateY(0)' }, '50%': { transform: 'translateY(-8px)' } },
        shimmer: { '100%': { transform: 'translateX(100%)' } },
      },
      animation: {
        'fade-up': 'fade-up .5s cubic-bezier(.22,1,.36,1) both',
        'scale-in': 'scale-in .35s cubic-bezier(.22,1,.36,1) both',
        float: 'float 6s ease-in-out infinite',
      },
    },
  },
  plugins: [],
};
