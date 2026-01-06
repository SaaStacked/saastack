import React, { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useLocation } from 'react-router-dom';
import { LogoutAction } from '../../../subDomains/identity/actions/logout.ts';
import { Organization, UserProfileForCaller } from '../../api/apiHost1';
import { RoutePaths } from '../../constants.ts';
import { useCurrentUser } from '../../providers/CurrentUserContext';
import Icon from '../icon/Icon.tsx';


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

// Creates a main navigation menu for the website, for authenticated users
// Displays a logo, navigation links
// Displays a user menu with a logout button
// Displays a mobile menu for small screens
export const MainMenu: React.FC = () => {
  const { t: translate } = useTranslation();
  const { profile, organization } = useCurrentUser();
  const { execute: logout } = LogoutAction();
  const location = useLocation();
  const [isDropdownOpen, setIsDropdownOpen] = useState(false);
  const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

  const navItems = [
    { path: '/cars/search', label: translate('components.navigation.main_menu.links.search_cars') },
    { path: '/bookings/reserve', label: translate('components.navigation.main_menu.links.reserve_car') }
  ];

  const isActive = (path: string) => location.pathname === path;

  return (
    <nav className="bg-white dark:bg-gray-800 shadow-sm border-b border-b-accent">
      <div className="container mx-auto px-4 max-w-4xl">
        <div className="flex justify-between items-center h-16">
          <div className="flex items-center space-x-2">
            <OrganizationAvatar organization={organization} />
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
          </div>
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
          <UserAvatar
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
        <div className="md:hidden pb-4 border-t border-gray-200 dark:border-gray-600">
          <div className="flex flex-col space-y-2 pt-4">
            {navItems.map((item) => (
              <Link
                key={item.path}
                to={item.path}
                className={`px-3 py-2 text-sm font-medium transition-colors ${
                  isActive(item.path)
                    ? 'text-primary dark:text-primary-light bg-blue-50 dark:bg-blue-900/20'
                    : 'text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100'
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
      <Link to={RoutePaths.Home} className="hidden md:flex items-center space-x-2 no-underline hover:no-underline">
        <img src="/images/logo.png" alt={translate('components.navigation.main_menu.logo')} className="w-8 h-8" />
        <span className="font-bold text-xl text-gray-900 dark:text-gray-100">
          {translate('components.navigation.main_menu.logo')}
        </span>
      </Link>

      <Link to={RoutePaths.Home} className="md:hidden no-underline hover:no-underline">
        <span className="font-bold text-xl text-gray-900 dark:text-gray-100">
          {translate('components.navigation.main_menu.logo')}
        </span>
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
              isActive(item.path)
                ? 'text-primary dark:text-primary-light border-b-2 border-primary dark:border-primary-light'
                : 'text-gray-600 dark:text-gray-300 hover:text-gray-900 dark:hover:text-gray-100'
            }`}
          >
            {item.label}
          </Link>
        ))}
      </div>
    </>
  );
}

function UserAvatar({
  translate,
  isDropdownOpen,
  setIsDropdownOpen,
  profile,
  logout
}: NavigationProps & {
  profile: any;
  logout: any;
}) {
  const displayName = profile?.displayName ? profile?.displayName : profile?.name.firstName || '?';
  const avatarLetter = displayName.charAt(0);
  return (
    <>
      <div className="relative">
        <button
          onClick={() => setIsDropdownOpen(!isDropdownOpen)}
          className="flex items-center space-x-2 p-2 rounded-full hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
        >
          {profile.avatarUrl ? (
            <img
              className="w-10 h-10 rounded-full border-1 object-cover border-gray-200 dark:border-gray-600"
              src={profile.avatarUrl}
              alt={displayName}
            />
          ) : (
            <div className="w-8 h-8 bg-accent rounded-full flex items-center justify-center">
              <span className="text-md font-medium text-white">{avatarLetter}</span>
            </div>
          )}
        </button>

        {isDropdownOpen && (
          <div className="absolute right-0 mt-2 w-48 bg-white dark:bg-gray-800 rounded-md shadow-lg border border-gray-200 dark:border-gray-600 z-50">
            <div className="py-1">
              <Link
                to={RoutePaths.UserProfile}
                className="block px-4 py-2 text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700"
                onClick={() => setIsDropdownOpen(false)}
              >
                {translate('components.navigation.main_menu.links.profile')}
              </Link>
              <Link
                to={RoutePaths.Organizations}
                className="block px-4 py-2 text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700"
                onClick={() => setIsDropdownOpen(false)}
              >
                {translate('components.navigation.main_menu.links.organizations')}
              </Link>
              <button
                onClick={() => {
                  setIsDropdownOpen(false);
                  logout();
                }}
                className="block w-full text-left px-4 py-2 text-sm text-gray-700 dark:text-gray-200 hover:bg-gray-100 dark:hover:bg-gray-700"
              >
                {translate('components.navigation.main_menu.links.logout')}
              </button>
            </div>
          </div>
        )}
      </div>
    </>
  );
}

function OrganizationAvatar({ organization }: { organization?: Organization }) {
  const orgName = organization?.name || 'Org';
  const avatarUrl = organization?.avatarUrl;

  return (
    <Link to={RoutePaths.Organizations} className="flex items-center">
      {avatarUrl ? (
        <img
          src={avatarUrl}
          alt={orgName}
          className="w-10 h-10 rounded-full object-cover border border-gray-200 dark:border-gray-600"
        />
      ) : (
        <Icon className="w-10 h-10 rounded-full object-cover bg-gray-200" symbol="company" size={36} color="gray-400" />
      )}
    </Link>
  );
}
