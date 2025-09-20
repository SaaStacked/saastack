import { Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom';
import './main.css';
import React, { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Layout } from './components/layout/Layout';
import Loader from './components/loader/Loader.tsx';
import { OfflineBanner } from './components/offline/OfflineBanner';
import { AboutPage } from './pages/about.tsx';
import { BookingsReservePage } from './pages/bookings/BookingsReserve.tsx';
import { CarsSearchPage } from './pages/cars/CarsSearch.tsx';
import { HomeAnonymousPage } from './pages/homeAnonymous.tsx';
import { HomeAuthenticatedPage } from './pages/homeAuthenticated.tsx';
import { CredentialsLoginPage } from './pages/identity/credentialsLogin.tsx';
import { CredentialsRegisterPage } from './pages/identity/credentialsRegister.tsx';
import { CredentialsRegisterConfirm } from './pages/identity/credentialsRegisterConfirm.tsx';
import { CredentialsRegisterRedirect } from './pages/identity/credentialsRegisterRedirect.tsx';
import { SsoMicrosoftPage } from './pages/identity/ssoMicrosoft.tsx';
import { OrganizationsManagePage } from './pages/organizations/OrganizationsManage.tsx';
import { PrivacyPage } from './pages/privacy.tsx';
import { TermsPage } from './pages/terms.tsx';
import { ProfileManagePage } from './pages/userProfiles/ProfileManage.tsx';
import { useCurrentUser } from './providers/CurrentUserContext.tsx';
import { recorder } from './recorder.ts';


const AuthenticatedOnlyRoutes: React.FC<{ isAuthenticated: boolean }> = ({ isAuthenticated }) =>
  isAuthenticated ? <Outlet /> : <Navigate to="/" replace />;
const AnonymousOnlyRoutes: React.FC<{ isAuthenticated: boolean }> = ({ isAuthenticated }) =>
  isAuthenticated ? <Navigate to="/" replace /> : <Outlet />;

const App: React.FC = () => {
  const { isExecuting, isAuthenticated } = useCurrentUser();
  const location = useLocation();
  const { ready } = useTranslation();

  useEffect(() => {
    if (ready) {
      recorder.trackPageView(location.pathname);
    }
  }, [location, ready]);

  if (isExecuting || ready === false) {
    return <Loader message="Loading..." />;
  }

  return (
    <>
      <OfflineBanner />
      <Layout>
        <Routes>
          <Route element={<AuthenticatedOnlyRoutes isAuthenticated={isAuthenticated} />}>
            <Route path="/cars/search" element={<CarsSearchPage />} />
            <Route path="/bookings/reserve" element={<BookingsReservePage />} />
            <Route path="/profile" element={<ProfileManagePage />} />
            <Route path="/organizations" element={<OrganizationsManagePage />} />
          </Route>

          <Route element={<AnonymousOnlyRoutes isAuthenticated={isAuthenticated} />}>
            <Route path="/identity/credentials/login" element={<CredentialsLoginPage />} />
            <Route path="/identity/sso/microsoft" element={<SsoMicrosoftPage />} />
            <Route path="/identity/credentials/register" element={<CredentialsRegisterPage />} />
            <Route path="/identity/credentials/register-confirm" element={<CredentialsRegisterConfirm />} />
            <Route path="/identity/credentials/register-redirect" element={<CredentialsRegisterRedirect />} />
          </Route>

          <Route path="/about" element={<AboutPage />} />
          <Route path="/privacy" element={<PrivacyPage />} />
          <Route path="/terms" element={<TermsPage />} />
          <Route path="/" element={isAuthenticated ? <HomeAuthenticatedPage /> : <HomeAnonymousPage />} />

          <Route path="*" element={<Navigate to="/" replace />} />
        </Routes>
      </Layout>
    </>
  );
};

export default App;
