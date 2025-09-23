import { useTheme } from '../../providers/ThemeContext.tsx';


// Creates a control to toggle the theme from light to dark
export function ThemeToggle() {
  const { theme, setTheme } = useTheme();

  return (
    <button
      onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
      aria-label="Toggle theme"
      title={`Toggle ${theme === 'dark' ? 'light' : 'dark'} mode`}
      className="p-2 rounded-md bg-gray-500 dark:bg-gray-500 text-gray-800 dark:text-gray-200 hover:bg-gray-300 dark:hover:bg-gray-600"
    >
      {theme === 'dark' ? '☀️' : '🌙'}
    </button>
  );
}
