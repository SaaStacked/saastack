import React from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { RoutePaths } from '../../constants.ts';
import { ThemeToggle } from '../theme/ThemeToggle';


// Creates a footer at the bottom of the layout
// Defines a set of links and tools
export const Footer: React.FC = () => {
  const currentYear = new Date().getFullYear();
  const { t: translate } = useTranslation();

  return (
    <footer className="relative bg-white dark:bg-neutral-800 border-t border-neutral-200 dark:border-neutral-700">
      <div className="container mx-auto px-4 py-2 max-w-4xl">
        <div className="flex flex-col md:flex-row justify-between items-center space-y-4 md:space-y-0">
          <div className="flex space-x-6 text-xs">
            <Link
              to={RoutePaths.Home}
              className="text-neutral-600 dark:text-neutral-400 hover:text-neutral-900 dark:hover:text-neutral-200 no-underline"
            >
              {translate('components.layout.footer.links.home')}
            </Link>
            <Link
              to={RoutePaths.About}
              className="text-neutral-600 dark:text-neutral-400 hover:text-neutral-900 dark:hover:text-neutral-200 no-underline"
            >
              {translate('components.layout.footer.links.about')}
            </Link>
            <Link
              to={RoutePaths.Privacy}
              className="text-neutral-600 dark:text-neutral-400 hover:text-neutral-900 dark:hover:text-neutral-200 no-underline"
            >
              {translate('components.layout.footer.links.privacy')}
            </Link>
            <Link
              to={RoutePaths.Terms}
              className="text-neutral-600 dark:text-neutral-400 hover:text-neutral-900 dark:hover:text-neutral-200 no-underline"
            >
              {translate('components.layout.footer.links.terms')}
            </Link>
          </div>

          <div className="flex items-center space-x-4">
            <div className="text-neutral-500 dark:text-neutral-400 text-xs">
              {translate('components.layout.footer.copyright', { currentYear })}
            </div>
            <ThemeToggle />
          </div>
        </div>
      </div>
    </footer>
  );
};
