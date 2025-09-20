import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation } from 'react-router-dom';
import { LogoutAction } from '../../actions/identity/logout.ts';
import { UserProfileForCaller } from '../../api/apiHost1';
import { useCurrentUser } from '../../providers/CurrentUserContext';


// Creates a main navigation bar for the website
// Displays a logo, navigation links
// Displays a user menu with a logout button
export const MainNavigation: React.FC = () => {
  const { t: translate } = useTranslation();
  const { profile } = useCurrentUser();
  const { execute: logout } = LogoutAction();
  const location = useLocation();
  const [isDropdownOpen, setIsDropdownOpen] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const navItems = [
    { path: '/cars/search', label: translate('components.layout.main_navigation.links.search_cars') },
    { path: '/bookings/reserve', label: translate('components.layout.main_navigation.links.reserve_car') }
  ];

  const isActive = (path: string) => location.pathname === path;

  return (
    <nav
      className="bg-white shadow-sm border-b border-b-accent
  "
    >
      <div className="container mx-auto px-4 max-w-4xl">
        <div className="flex justify-between items-center h-16">
          <MobileHamburgerMenu
            translate={translate}
            isMobileMenuOpen={isMobileMenuOpen}
            setIsMobileMenuOpen={setIsMobileMenuOpen}
            navItems={navItems}
            isActive={isActive}
            isDropdownOpen={isDropdownOpen}
            setIsDropdownOpen={setIsDropdownOpen}
            profile={profile}
            logout={logout}
          />
          <BrandText
            translate={translate}
            isMobileMenuOpen={isMobileMenuOpen}
            setIsMobileMenuOpen={setIsMobileMenuOpen}
            navItems={navItems}
            isActive={isActive}
            isDropdownOpen={isDropdownOpen}
            setIsDropdownOpen={setIsDropdownOpen}
            profile={profile}
            logout={logout}
          />
          <DesktopNavLinks
            translate={translate}
            isMobileMenuOpen={isMobileMenuOpen}
            setIsMobileMenuOpen={setIsMobileMenuOpen}
            navItems={navItems}
            isActive={isActive}
            isDropdownOpen={isDropdownOpen}
            setIsDropdownOpen={setIsDropdownOpen}
            profile={profile}
            logout={logout}
          />
          <UserMenu
            translate={translate}
            isMobileMenuOpen={isMobileMenuOpen}
            setIsMobileMenuOpen={setIsMobileMenuOpen}
            navItems={navItems}
            isActive={isActive}
            isDropdownOpen={isDropdownOpen}
            setIsDropdownOpen={setIsDropdownOpen}
            profile={profile}
            logout={logout}
          />
        </div>

        <MobileDropdownMenu
          translate={translate}
          isMobileMenuOpen={isMobileMenuOpen}
          setIsMobileMenuOpen={setIsMobileMenuOpen}
          navItems={navItems}
          isActive={isActive}
          isDropdownOpen={isDropdownOpen}
          setIsDropdownOpen={setIsDropdownOpen}
          profile={profile}
          logout={logout}
        />
      </div>
    </nav>
  );
};

interface NavigationProps {
  isMobileMenuOpen: boolean;
  setIsMobileMenuOpen: (value: boolean) => void;
  translate: (key: string) => string;
  isActive: (path: string) => boolean;
  navItems: { path: string; label: string }[];
  isDropdownOpen: boolean;
  setIsDropdownOpen: (value: boolean) => void;
  profile: UserProfileForCaller;
  logout: () => void;
}

function MobileHamburgerMenu({ isMobileMenuOpen, setIsMobileMenuOpen }: NavigationProps) {
  return (
    <>
      <button className="md:hidden p-2" onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}>
        <svg className="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
        </svg>
      </button>
    </>
  );
}

function MobileDropdownMenu({ isMobileMenuOpen, setIsMobileMenuOpen, navItems, isActive }: NavigationProps) {
  return (
    <>
      {isMobileMenuOpen && (
        <div className="md:hidden pb-4 border-t">
          <div className="flex flex-col space-y-2 pt-4">
            {navItems.map((item) => (
              <Link
                key={item.path}
                to={item.path}
                className={`px-3 py-2 text-sm font-medium transition-colors ${
                  isActive(item.path) ? 'text-primary bg-blue-50' : 'text-gray-600 hover:text-gray-900'
                }`}
                onClick={() => setIsMobileMenuOpen(false)}
              >
                {item.label}
              </Link>
            ))}
          </div>
        </div>
      )}
    </>
  );
}

function BrandText({ translate }: NavigationProps) {
  return (
    <>
      <Link to="/" className="hidden md:flex items-center space-x-2 no-underline hover:no-underline">
        <img src="/images/logo.png" alt={translate('components.layout.main_navigation.logo')} className="w-8 h-8" />
        <span className="font-bold text-xl text-gray-900">{translate('components.layout.main_navigation.logo')}</span>
      </Link>

      <Link to="/" className="md:hidden no-underline hover:no-underline">
        <span className="font-bold text-xl text-gray-900">{translate('components.layout.main_navigation.logo')}</span>
      </Link>
    </>
  );
}

function DesktopNavLinks({ navItems, isActive }: NavigationProps) {
  return (
    <>
      <div className="hidden md:flex space-x-8">
        {navItems.map((item) => (
          <Link
            key={item.path}
            to={item.path}
            className={`px-3 py-2 text-sm font-medium transition-colors ${
              isActive(item.path) ? 'text-primary border-b-2 border-primary' : 'text-gray-600 hover:text-gray-900'
            }`}
          >
            {item.label}
          </Link>
        ))}
      </div>
    </>
  );
}

function UserMenu({
  translate,
  isDropdownOpen,
  setIsDropdownOpen,
  profile,
  logout
}: NavigationProps & {
  profile: any;
  logout: any;
}) {
  return (
    <>
      <div className="relative">
        <button
          onClick={() => setIsDropdownOpen(!isDropdownOpen)}
          className="flex items-center space-x-2 p-2 rounded-full hover:bg-gray-100 transition-colors"
        >
          <div className="w-8 h-8 bg-accent rounded-full flex items-center justify-center">
            <span className="text-md font-medium text-white">{profile?.name.firstName?.charAt(0) || '?'}</span>
          </div>
        </button>

        {isDropdownOpen && (
          <div className="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg border z-50">
            <div className="py-1">
              <Link
                to="/profile"
                className="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                onClick={() => setIsDropdownOpen(false)}
              >
                {translate('components.layout.main_navigation.links.profile')}
              </Link>
              <Link
                to="/organizations"
                className="block px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
                onClick={() => setIsDropdownOpen(false)}
              >
                {translate('components.layout.main_navigation.links.organizations')}
              </Link>
              <button
                onClick={() => {
                  setIsDropdownOpen(false);
                  logout();
                }}
                className="block w-full text-left px-4 py-2 text-sm text-gray-700 hover:bg-gray-100"
              >
                {translate('components.layout.main_navigation.links.logout')}
              </button>
            </div>
          </div>
        )}
      </div>
    </>
  );
}
