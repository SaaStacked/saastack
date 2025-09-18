import { Navigate, Route, Routes, useLocation } from 'react-router-dom';
import './main.css';
import React, { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { OfflineBanner } from './components/offline/OfflineBanner';
import { HomeAnonymousPage } from './pages/homeAnonymous.tsx';
import { HomeAuthenticatedPage } from './pages/homeAuthenticated.tsx';
import { AfterRegisterCredentials } from './pages/identity/afterRegisterCredentials.tsx';
import { LoginCredentialsPage } from './pages/identity/loginCredentials.tsx';
import { RegisterCredentialsPage } from './pages/identity/registerCredentials.tsx';
import { SsoMicrosoftPage } from './pages/identity/ssoMicrosoft.tsx';
import { PrivacyPage } from './pages/privacy.tsx';
import { TermsPage } from './pages/terms.tsx';
import { useCurrentUser } from './providers/CurrentUserContext.tsx';
import { recorder } from './recorder.ts';


const App: React.FC = () => {
  const { isExecuting, isAuthenticated } = useCurrentUser();
  const location = useLocation();
  const { t: translate, ready } = useTranslation('common');

  useEffect(() => recorder.trackPageView(location.pathname), [location]);

  if (isExecuting || !ready) {
    return (
      <div className="flex flex-col items-center space-y-4 items-center justify-center h-screen">
        <div className="rounded-full h-12 w-12 border-4 border-gray-300 border-t-blue-500 animate-spin"></div>
        <p className="text-gray-600">{translate('app.loading')}</p>
      </div>
    );
  }

  return (
    <div className="min-h-screen font-sans bg-gray-200">
      <OfflineBanner />
      <main className="container mx-auto px-4 py-8 max-w-4xl">
        <Routes>
          <Route path="/" element={<HomeAnonymousPage />} />
          <Route path="/privacy" element={<PrivacyPage />} />
          <Route path="/terms" element={<TermsPage />} />
          <Route
            path="/identity/login-credentials"
            element={isAuthenticated ? <Navigate to="/" replace /> : <LoginCredentialsPage />}
          />
          <Route
            path="/identity/sso-microsoft"
            element={isAuthenticated ? <Navigate to="/" replace /> : <SsoMicrosoftPage />}
          />
          <Route
            path="/identity/register-credentials"
            element={isAuthenticated ? <Navigate to="/" replace /> : <RegisterCredentialsPage />}
          />
          <Route path="/identity/after-register-credentials" element={<AfterRegisterCredentials />} />
          <Route path="/user" element={<HomeAuthenticatedPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </main>
    </div>
  );
};

export default App;
