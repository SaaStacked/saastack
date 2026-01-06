import { Navigate, Outlet, Route, Routes, useLocation } from 'react-router-dom';
import './main.css';
import React, { useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Layout } from './framework/components/layout/Layout.tsx';
import Loader from './framework/components/loader/Loader.tsx';
import { OfflineBanner } from './framework/components/offline/OfflineBanner.tsx';
import { RoutePaths } from './framework/constants.ts';
import { useCurrentUser } from './framework/providers/CurrentUserContext.tsx';
import { recorder } from './framework/recorder.ts';
import { BookingsReservePage } from './subDomains/bookings/pages/BookingsReserve.tsx';
import { CarsSearchPage } from './subDomains/cars/pages/CarsSearch.tsx';
import { AboutPage } from './subDomains/home/pages/About.tsx';
import { HomeAnonymousPage } from './subDomains/home/pages/HomeAnonymous.tsx';
import { HomeAuthenticatedPage } from './subDomains/home/pages/HomeAuthenticated.tsx';
import { PrivacyPage } from './subDomains/home/pages/Privacy.tsx';
import { TermsPage } from './subDomains/home/pages/Terms.tsx';
import { CredentialsLoginPage } from './subDomains/identity/pages/CredentialsLogin.tsx';
import { CredentialsRegisterPage } from './subDomains/identity/pages/CredentialsRegister.tsx';
import { CredentialsRegisterConfirm } from './subDomains/identity/pages/CredentialsRegisterConfirm.tsx';
import { CredentialsRegisterRedirect } from './subDomains/identity/pages/CredentialsRegisterRedirect.tsx';
import { OAuth2AuthorizePage } from './subDomains/identity/pages/OAuth2Authorize.tsx';
import { OAuth2ConsentClientPage } from './subDomains/identity/pages/OAuth2ConsentClient.tsx';
import { PasswordResetCompletePage } from './subDomains/identity/pages/PasswordResetComplete.tsx';
import { PasswordResetRequestPage } from './subDomains/identity/pages/PasswordResetInitiate.tsx';
import { PasswordResetRedirectPage } from './subDomains/identity/pages/PasswordResetRedirect.tsx';
import { SsoLoginPage } from './subDomains/identity/pages/SsoLoginPage.tsx';
import { OrganizationEditPage } from './subDomains/organizations/pages/OrganizationEditPage.tsx';
import { OrganizationNewPage } from './subDomains/organizations/pages/OrganizationNewPage.tsx';
import { OrganizationsManagePage } from './subDomains/organizations/pages/OrganizationsManagePage.tsx';
import { ProfilesManagePage } from './subDomains/userProfiles/pages/ProfilesManage.tsx';

const AuthenticatedOnlyRoutes: React.FC<{ isAuthenticated: boolean }> = ({ isAuthenticated }) =>
  isAuthenticated ? <Outlet /> : <Navigate to={RoutePaths.Home} replace />;
const AnonymousOnlyRoutes: React.FC<{ isAuthenticated: boolean }> = ({ isAuthenticated }) =>
  isAuthenticated ? <Navigate to={RoutePaths.Home} replace /> : <Outlet />;

// Routes that should not display the main menu
export const MainMenuExcludedRoutes = [
  // Authenticated only routes
  RoutePaths.OAuth2ConsentClient,

  // Anonymous only routes
  RoutePaths.CredentialsLogin,
  RoutePaths.SsoMicrosoft,
  RoutePaths.SsoGoogle,
  RoutePaths.Register,
  RoutePaths.RegisterRedirect,
  RoutePaths.RegisterConfirm,
  RoutePaths.PasswordReset,
  RoutePaths.PasswordResetComplete,
  RoutePaths.PasswordResetRedirect,

  // Authenticated or Anonymous routes
  RoutePaths.About,
  RoutePaths.Privacy,
  RoutePaths.Terms,
  RoutePaths.OAuth2Authorize,
  RoutePaths.OAuth2AuthorizeAlt
];

const PageTitles: Record<string, string | { key: string; params: any }> = {
  // Authenticated only routes
  [RoutePaths.OAuth2ConsentClient]: 'pages.identity.oauth2_consent_client.title',
  '/cars/search': 'pages.cars.search.title',
  '/bookings/reserve': 'pages.bookings.reserve.title',
  [RoutePaths.UserProfile]: 'pages.profiles.manage.title',
  [RoutePaths.Organizations]: 'pages.organizations.manage.title',
  [RoutePaths.OrganizationsNew]: 'pages.organizations.new.title',
  [RoutePaths.OrganizationsEdit]: 'pages.organizations.edit.title',

  // Anonymous only routes
  [RoutePaths.CredentialsLogin]: 'pages.identity.credentials_login.title',
  [RoutePaths.SsoMicrosoft]: { key: 'pages.identity.sso_login.title', params: { provider: 'Microsoft' } },
  [RoutePaths.SsoGoogle]: { key: 'pages.identity.sso_login.title', params: { provider: 'Google' } },
  [RoutePaths.Register]: 'pages.identity.credentials_register.title',
  [RoutePaths.RegisterConfirm]: 'pages.identity.credentials_register_confirm.title',
  [RoutePaths.RegisterRedirect]: 'pages.identity.credentials_register_redirect.title',
  [RoutePaths.PasswordReset]: 'pages.identity.credentials_password_reset_initiate.title',
  [RoutePaths.PasswordResetComplete]: 'pages.identity.credentials_password_reset_complete.title',
  [RoutePaths.PasswordResetRedirect]: 'pages.identity.credentials_password_reset_redirect.title',

  // Authenticated or Anonymous routes
  [RoutePaths.About]: 'pages.about.title',
  [RoutePaths.Privacy]: 'pages.privacy.title',
  [RoutePaths.Terms]: 'pages.terms.title',
  [RoutePaths.OAuth2Authorize]: 'pages.identity.oauth2_authorize.title',
  [RoutePaths.OAuth2AuthorizeAlt]: 'pages.identity.oauth2_authorize.title'
};

const App: React.FC = () => {
  const { isExecuting, isAuthenticated } = useCurrentUser();
  const location = useLocation();
  const { ready, t: translate } = useTranslation();

  useEffect(() => {
    if (ready) {
      recorder.trackPageView(location.pathname);
      const pageTitle = PageTitles[location.pathname];
      if (pageTitle) {
        const titleKey = typeof pageTitle === 'string' ? pageTitle : pageTitle.key;
        const titleParams = typeof pageTitle === 'string' ? {} : pageTitle.params;
        const documentTitle = translate(titleKey, titleParams);
        document.title = `${documentTitle} - ${translate('app.name')}`;
      }
    }
  }, [location, ready, translate]);

  if (isExecuting || ready === false) {
    return <Loader type="page" message="Loading" />;
  }

  return (
    <>
      <OfflineBanner />
      <Layout>
        <Routes>
          {/* Authenticated only routes */}
          <Route element={<AuthenticatedOnlyRoutes isAuthenticated={isAuthenticated} />}>
            <Route path={RoutePaths.OAuth2ConsentClient} element={<OAuth2ConsentClientPage />} />
            <Route path="/cars/search" element={<CarsSearchPage />} />
            <Route path="/bookings/reserve" element={<BookingsReservePage />} />
            <Route path={RoutePaths.UserProfile} element={<ProfilesManagePage />} />
            <Route path={RoutePaths.Organizations} element={<OrganizationsManagePage />} />
            <Route path={RoutePaths.OrganizationsNew} element={<OrganizationNewPage />} />
            <Route path={RoutePaths.OrganizationsEdit} element={<OrganizationEditPage />} />
          </Route>

          {/* Anonymous only routes */}
          <Route element={<AnonymousOnlyRoutes isAuthenticated={isAuthenticated} />}>
            <Route path={RoutePaths.CredentialsLogin} element={<CredentialsLoginPage />} />
            {window.isTestingOnly && (
              <Route
                path="/identity/sso/fakeprovider"
                element={
                  <SsoLoginPage
                    providerId="fakesso"
                    providerName="Fake SSO Provider"
                    authorizationServerBaseUrl={import.meta.env.VITE_FAKEPROVIDER_SSO_BASEURL}
                  />
                }
              />
            )}
            <Route
              path={RoutePaths.SsoMicrosoft}
              element={
                <SsoLoginPage
                  providerId="microsoft"
                  providerName="Microsoft"
                  pkce={true}
                  authorizationServerBaseUrl={import.meta.env.VITE_MICROSOFT_SSO_BASEURL}
                  clientId={import.meta.env.VITE_MICROSOFT_SSO_CLIENTID}
                />
              }
            />
            <Route
              path={RoutePaths.SsoGoogle}
              element={
                <SsoLoginPage
                  providerId="google"
                  providerName="Google"
                  pkce={true}
                  authorizationServerBaseUrl={import.meta.env.VITE_GOOGLE_SSO_BASEURL}
                  clientId={import.meta.env.VITE_GOOGLE_SSO_CLIENTID}
                />
              }
            />
            <Route path={RoutePaths.Register} element={<CredentialsRegisterPage />} />
            <Route path={RoutePaths.RegisterConfirm} element={<CredentialsRegisterConfirm />} />
            <Route path={RoutePaths.RegisterRedirect} element={<CredentialsRegisterRedirect />} />
            <Route path={RoutePaths.PasswordReset} element={<PasswordResetRequestPage />} />
            <Route path={RoutePaths.PasswordResetComplete} element={<PasswordResetCompletePage />} />
            <Route path={RoutePaths.PasswordResetRedirect} element={<PasswordResetRedirectPage />} />
          </Route>

          {/* Authenticated or Anonymous routes */}
          <Route path={RoutePaths.Home} element={isAuthenticated ? <HomeAuthenticatedPage /> : <HomeAnonymousPage />} />
          <Route path={RoutePaths.About} element={<AboutPage />} />
          <Route path={RoutePaths.Privacy} element={<PrivacyPage />} />
          <Route path={RoutePaths.Terms} element={<TermsPage />} />
          <Route path={RoutePaths.OAuth2Authorize} element={<OAuth2AuthorizePage />} />
          <Route path={RoutePaths.OAuth2AuthorizeAlt} element={<OAuth2AuthorizePage />} />

          {/* Must be last route */}
          <Route path="*" element={<Navigate to={RoutePaths.Home} replace />} />
        </Routes>
      </Layout>
    </>
  );
};

export default App;
