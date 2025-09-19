import { Navigate, Route, Routes, useLocation } from 'react-router-dom';
import './main.css';
import React, { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Layout } from './components/layout/Layout';
import Loader from './components/loader/Loader.tsx';
import { OfflineBanner } from './components/offline/OfflineBanner';
import { HomeAnonymousPage } from './pages/homeAnonymous.tsx';
import { HomeAuthenticatedPage } from './pages/homeAuthenticated.tsx';
import { CredentialsLoginPage } from './pages/identity/credentialsLogin.tsx';
import { CredentialsRegisterPage } from './pages/identity/credentialsRegister.tsx';
import { CredentialsRegisterConfirm } from './pages/identity/credentialsRegisterConfirm.tsx';
import { CredentialsRegisterRedirect } from './pages/identity/credentialsRegisterRedirect.tsx';
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
    return <Loader message={translate('app.loading')} />;
  }

  return (
    <>
      <OfflineBanner />
      <Layout>
        <Routes>
          <Route path="/privacy" element={<PrivacyPage />} />
          <Route path="/terms" element={<TermsPage />} />
          <Route
            path="/identity/credentials/login"
            element={isAuthenticated ? <Navigate to="/" replace /> : <CredentialsLoginPage />}
          />
          <Route
            path="/identity/sso/microsoft"
            element={isAuthenticated ? <Navigate to="/" replace /> : <SsoMicrosoftPage />}
          />
          <Route
            path="/identity/credentials/register"
            element={isAuthenticated ? <Navigate to="/" replace /> : <CredentialsRegisterPage />}
          />
          <Route
            path="/identity/credentials/register-confirm"
            element={isAuthenticated ? <Navigate to="/" replace /> : <CredentialsRegisterConfirm />}
          />
          <Route path="/identity/credentials/register-redirect" element={<CredentialsRegisterRedirect />} />
          <Route path="/" element={isAuthenticated ? <HomeAuthenticatedPage /> : <HomeAnonymousPage />} />
          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Layout>
    </>
  );
};

export default App;
