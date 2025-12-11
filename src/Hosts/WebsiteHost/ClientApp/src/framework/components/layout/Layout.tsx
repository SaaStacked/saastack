import React, { useEffect, useRef, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { useCurrentUser } from '../../providers/CurrentUserContext';
import { Footer } from './Footer.tsx';
import { MainMenu } from '../navigation/MainMenu.tsx';
import { toClasses } from '../Components.ts';


interface LayoutProps {
  children: React.ReactNode;
}

const EXCLUDED_ANONYMOUS_ROUTES = [
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
  const [needsBottomPadding, setNeedsBottomPadding] = useState(false);
  const mainRef = useRef<HTMLElement>(null);

  const shouldShowNavigation = isAuthenticated && !EXCLUDED_ANONYMOUS_ROUTES.includes(location.pathname);

  useEffect(() => {
    const checkContentHeight = () => {
      if (mainRef.current) {
        const mainRect = mainRef.current.getBoundingClientRect();
        const viewportHeight = window.innerHeight;
        const contentReachesBottom = mainRect.bottom > viewportHeight;
        setNeedsBottomPadding(contentReachesBottom);
      }
    };

    checkContentHeight();
    window.addEventListener('resize', checkContentHeight);

    return () => window.removeEventListener('resize', checkContentHeight);
  }, [children]);

  const baseClasses = `px-4 py-8 ${shouldShowNavigation ? 'pt-4' : ''} ${needsBottomPadding ? 'pb-48' : ''}`;
  const widthClasses = 'container mx-auto max-w-4xl lg:max-w-none';
  const classes = toClasses([baseClasses, widthClasses]);

  return (
    <div className="min-h-screen font-sans bg-gray-200 dark:bg-gray-900 text-gray-900 dark:text-gray-100">
      {shouldShowNavigation && <MainMenu />}
      <main
        ref={mainRef}
        className={classes}
      >
        {children}
      </main>
      <Footer />
    </div>
  );
};
