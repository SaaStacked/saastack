import React from 'react';
import { useLocation } from 'react-router-dom';
import { useCurrentUser } from '../../providers/CurrentUserContext';
import { Footer } from './Footer.tsx';
import { MainNavigation } from './MainNavigation';


interface LayoutProps {
  children: React.ReactNode;
}

const EXCLUDED_ROUTES = [
  '/about',
  '/privacy',
  '/terms',
  '/identity/credentials/login',
  '/identity/sso/microsoft',
  '/identity/credentials/register',
  '/identity/credentials/register-confirm',
  '/identity/credentials/register-redirect'
];

// Creates the main layout of all pages
// Displays the main navigation bar at the top if the page is not excluded
// Displays the footer at the bottom of the page
export const Layout: React.FC<LayoutProps> = ({ children }) => {
  const { isAuthenticated } = useCurrentUser();
  const location = useLocation();

  const shouldShowNavigation = isAuthenticated && !EXCLUDED_ROUTES.includes(location.pathname);

  return (
    <div className="min-h-screen font-sans bg-gray-200 dark:bg-gray-900 text-gray-900 dark:text-gray-100 flex flex-col">
      {shouldShowNavigation && <MainNavigation />}
      <main className={`container mx-auto px-4 py-8 max-w-4xl ${shouldShowNavigation ? 'pt-4' : ''}`}>{children}</main>
      <Footer />
    </div>
  );
};
