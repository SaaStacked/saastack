import React from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import { RoutePaths } from '../../constants.ts';

// Creates a simple brand bar at the top of the page
export const BrandBar: React.FC = () => {
  const { t: translate } = useTranslation();

  return (
    <nav className="flex shadow-sm border-b border-brand-primary-400">
      <div className="w-[50px] bg-brand-primary-600 flex items-center justify-center py-1">
        <Link to={RoutePaths.Home}>
          <img src="/images/logo.svg" alt={translate('app.name')} className="h-8 w-auto" />
        </Link>
      </div>

      <div className="flex-1 bg-brand-primary"></div>
    </nav>
  );
};
