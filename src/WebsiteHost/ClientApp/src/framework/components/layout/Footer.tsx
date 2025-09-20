import React from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';


export const Footer: React.FC = () => {
  const currentYear = new Date().getFullYear();
  const { t: translate } = useTranslation();

  return (
    <footer className="bg-white border-t border-gray-200 mt-auto">
      <div className="container mx-auto px-4 py-2 max-w-4xl">
        <div className="flex flex-col md:flex-row justify-between items-center space-y-4 md:space-y-0">
          <div className="flex space-x-6  text-xs">
            <Link to="/" className="text-gray-600 hover:text-gray-900 no-underline">
              {translate('components.layout.footer.links.home')}
            </Link>
            <Link to="/about" className="text-gray-600 hover:text-gray-900 no-underline">
              {translate('components.layout.footer.links.about')}
            </Link>
            <Link to="/privacy" className="text-gray-600 hover:text-gray-900 no-underline">
              {translate('components.layout.footer.links.privacy')}
            </Link>
            <Link to="/terms" className="text-gray-600 hover:text-gray-900 no-underline">
              {translate('components.layout.footer.links.terms')}
            </Link>
          </div>
          <div className="text-gray-500 text-xs">
            {translate('components.layout.footer.copyright', { currentYear })}
          </div>
        </div>
      </div>
    </footer>
  );
};
