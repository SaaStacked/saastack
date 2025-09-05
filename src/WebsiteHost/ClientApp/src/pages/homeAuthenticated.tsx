import { LogoutAction } from '../actions/identity/logoutUser.ts';
import { useCurrentUser } from '../providers/CurrentUserContext.tsx';


export function HomeAuthenticatedPage() {
  const { profile } = useCurrentUser();
  const { execute: logout } = LogoutAction();

  return (
    <div className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="bg-white shadow">
        <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
          <div className="flex justify-between items-center py-6">
            <div className="flex items-center">
              <h1 className="text-3xl font-bold text-gray-900">Welcome to SaaStack</h1>
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
                Logout
              </button>
            </div>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <main className="max-w-7xl mx-auto py-6 sm:px-6 lg:px-8">
        <div className="px-4 py-6 sm:px-0">
          <div className="border-4 border-dashed border-gray-200 rounded-lg p-8">
            <div className="text-center">
              <h2 className="text-2xl font-bold text-gray-900 mb-4">
                Hello, {profile?.displayName || profile?.name.firstName || 'there'}! 👋
              </h2>
              <p className="text-lg text-gray-600 mb-8">
                You are successfully authenticated and viewing your home page.
              </p>

              {/* User Info Card */}
              <div className="bg-white rounded-lg shadow p-6 max-w-md mx-auto">
                <h3 className="text-lg font-semibold text-gray-900 mb-4">Your Profile</h3>
                <div className="space-y-2 text-left">
                  {profile?.displayName && (
                    <div>
                      <span className="font-medium text-gray-700">Display Name:</span>
                      <span className="ml-2 text-gray-600">{profile.displayName}</span>
                    </div>
                  )}
                  {profile?.emailAddress && (
                    <div>
                      <span className="font-medium text-gray-700">Email:</span>
                      <span className="ml-2 text-gray-600">{profile.emailAddress}</span>
                    </div>
                  )}
                  {profile?.phoneNumber && (
                    <div>
                      <span className="font-medium text-gray-700">Phone:</span>
                      <span className="ml-2 text-gray-600">{profile.phoneNumber}</span>
                    </div>
                  )}
                  {profile?.timezone && (
                    <div>
                      <span className="font-medium text-gray-700">Timezone:</span>
                      <span className="ml-2 text-gray-600">{profile.timezone}</span>
                    </div>
                  )}
                  {profile?.address.countryCode && (
                    <div>
                      <span className="font-medium text-gray-700">Country:</span>
                      <span className="ml-2 text-gray-600">{profile.address.countryCode}</span>
                    </div>
                  )}
                </div>
              </div>
            </div>
          </div>
        </div>
      </main>
    </div>
  );
}
