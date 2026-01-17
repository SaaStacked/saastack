import React, { useEffect, useRef, useState } from 'react';
import { useLocation } from 'react-router-dom';
import { MainMenuExcludedRoutes } from '../../../App.tsx';
import { useCurrentUser } from '../../providers/CurrentUserContext';
import { toClasses } from '../Components.ts';
import { BrandBar } from '../navigation/BrandBar.tsx';
import { MainMenu } from '../navigation/MainMenu.tsx';
import { Footer } from './Footer.tsx';


interface LayoutProps {
  children: React.ReactNode;
}

// Creates the main layout of all pages
// Displays the main navigation bar at the top if the page is not excluded
// Displays the footer at the bottom of the page
export const Layout: React.FC<LayoutProps> = ({ children }) => {
  const { isAuthenticated } = useCurrentUser();
  const location = useLocation();
  const [needsBottomPadding, setNeedsBottomPadding] = useState(false);
  const mainRef = useRef<HTMLElement>(null);

  const shouldShowMainMenu = isAuthenticated && !MainMenuExcludedRoutes.includes(location.pathname);

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

  const baseClasses = `px-4 py-8 ${shouldShowMainMenu ? 'pt-4' : ''} ${needsBottomPadding ? 'pb-48' : ''}`;
  const widthClasses = 'container mx-auto max-w-4xl lg:max-w-none';
  const classes = toClasses([baseClasses, widthClasses]);

  return (
    <div className="min-h-screen font-sans bg-neutral-200 dark:bg-neutral-900 text-neutral-900 dark:text-neutral-100">
      {shouldShowMainMenu ? <MainMenu /> : <BrandBar />}
      <main ref={mainRef} className={classes}>
        {children}
      </main>
      <Footer />
    </div>
  );
};
