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
    <footer className="relative bg-white dark:bg-gray-800 border-t border-gray-200 dark:border-gray-700">
      <div className="container mx-auto px-4 py-2 max-w-4xl">
        <div className="flex flex-col md:flex-row justify-between items-center space-y-4 md:space-y-0">
          <div className="flex space-x-6 text-xs">
            <Link
              to={RoutePaths.Home}
              className="text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 no-underline"
            >
              {translate('components.layout.footer.links.home')}
            </Link>
            <Link
              to={RoutePaths.About}
              className="text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 no-underline"
            >
              {translate('components.layout.footer.links.about')}
            </Link>
            <Link
              to={RoutePaths.Privacy}
              className="text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 no-underline"
            >
              {translate('components.layout.footer.links.privacy')}
            </Link>
            <Link
              to={RoutePaths.Terms}
              className="text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 no-underline"
            >
              {translate('components.layout.footer.links.terms')}
            </Link>
          </div>

          <div className="flex items-center space-x-4">
            <div className="text-gray-500 dark:text-gray-400 text-xs">
              {translate('components.layout.footer.copyright', { currentYear })}
            </div>
            <ThemeToggle />
          </div>
        </div>
      </div>
    </footer>
  );
};
