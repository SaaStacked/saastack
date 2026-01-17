import { useTheme } from '../../providers/ThemeContext.tsx';

// Creates a control to toggle the theme from light to dark
export function ThemeToggle() {
  const { theme, setTheme } = useTheme();

  return (
    <button
      onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
      aria-label="Toggle theme"
      title={`Toggle ${theme === 'dark' ? 'light' : 'dark'} mode`}
      className="p-2 rounded-md bg-neutral-500 dark:bg-neutral-500 text-neutral-800 dark:text-neutral-200 hover:bg-neutral-300 dark:hover:bg-neutral-600"
    >
      {theme === 'dark' ? '☀️' : '🌙'}
    </button>
  );
}
