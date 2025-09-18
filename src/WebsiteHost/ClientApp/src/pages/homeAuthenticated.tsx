import { useTranslation } from 'react-i18next';
import { LogoutAction } from '../actions/identity/logoutUser.ts';
import { useCurrentUser } from '../providers/CurrentUserContext.tsx';


export function HomeAuthenticatedPage() {
  const { profile } = useCurrentUser();
  const { execute: logout } = LogoutAction();
  const { t: translate } = useTranslation('common');

  return (
    <div className="min-h-screen bg-gray-50">
      <header className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-6">
            <div className="flex items-center">
              <h1 className="text-3xl font-bold text-gray-900">{translate('pages.home.home_authenticated.title')}</h1>
            </div>
            <div className="flex items-center space-x-4">
              {profile?.avatarUrl && (
                <img
                  className="h-8 w-8 rounded-full"
                  src={profile.avatarUrl}
                  alt={profile.displayName || 'User avatar'}
                />
              )}
              <span className="text-sm font-medium text-gray-700">
                {profile?.displayName || profile?.name.firstName || 'User'}
              </span>
              <button
                onClick={logout}
                className="bg-red-600 hover:bg-red-700 text-white px-4 py-2 rounded-md text-sm font-medium transition-colors"
              >
                {translate('pages.home.home_authenticated.links.logout')}
              </button>
            </div>
          </div>
        </div>
      </header>

      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8"></main>
    </div>
  );
}
