import { Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom';
import './main.css';
import React, { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Layout } from './framework/components/layout/Layout.tsx';
import Loader from './framework/components/loader/Loader.tsx';
import { OfflineBanner } from './framework/components/offline/OfflineBanner.tsx';
import { useCurrentUser } from './framework/providers/CurrentUserContext.tsx';
import { recorder } from './framework/recorder.ts';
import { BookingsReservePage } from './subDomains/bookings/pages/BookingsReserve.tsx';
import { CarsSearchPage } from './subDomains/cars/pages/CarsSearch.tsx';
import { AboutPage } from './subDomains/home/pages/about.tsx';
import { HomeAnonymousPage } from './subDomains/home/pages/homeAnonymous.tsx';
import { HomeAuthenticatedPage } from './subDomains/home/pages/homeAuthenticated.tsx';
import { PrivacyPage } from './subDomains/home/pages/privacy.tsx';
import { TermsPage } from './subDomains/home/pages/terms.tsx';
import { CredentialsLoginPage } from './subDomains/identity/pages/credentialsLogin.tsx';
import { CredentialsRegisterPage } from './subDomains/identity/pages/credentialsRegister.tsx';
import { CredentialsRegisterConfirm } from './subDomains/identity/pages/credentialsRegisterConfirm.tsx';
import { CredentialsRegisterRedirect } from './subDomains/identity/pages/credentialsRegisterRedirect.tsx';
import { SsoMicrosoftPage } from './subDomains/identity/pages/ssoMicrosoft.tsx';
import { OrganizationsManagePage } from './subDomains/organizations/pages/OrganizationsManage.tsx';
import { ProfileManagePage } from './subDomains/userProfiles/pages/ProfileManage.tsx';


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
