import { Navigate, Route, BrowserRouter as Router, Routes, useLocation } from 'react-router-dom';
import './main.css';
import React, { useEffect } from 'react';
import { HomeAnonymousPage } from './pages/homeAnonymous.tsx';
import { HomeAuthenticatedPage } from './pages/homeAuthenticated.tsx';
import { AfterRegisterCredentials } from './pages/identity/afterRegisterCredentials.tsx';
import { LoginCredentialsPage } from './pages/identity/loginCredentials.tsx';
import { LoginSsoGooglePage } from './pages/identity/loginSsoGoogle.tsx';
import { LoginSsoMicrosoftPage } from './pages/identity/loginSsoMicrosoft.tsx';
import { RegisterCredentialsPage } from './pages/identity/registerCredentials.tsx';
import { useCurrentUser } from './providers/CurrentUserContext.tsx';
import { recorder } from './recorder.ts';


const AppContent: React.FC = () => {
  const { isAuthenticated } = useCurrentUser();
  const location = useLocation();

  useEffect(() => recorder.trackPageView(location.pathname), [location]);

  return (
    <div className="min-h-screen font-sans">
      <main className="container mx-auto px-4 py-8 max-w-4xl">
        <Routes>
          <Route path="/" element={<HomeAnonymousPage />} />
          <Route
            path="/identity/login-credentials"
            element={isAuthenticated ? <Navigate to="/" replace /> : <LoginCredentialsPage />}
          />
          <Route
            path="/identity/login-sso-ms"
            element={isAuthenticated ? <Navigate to="/" replace /> : <LoginSsoMicrosoftPage />}
          />
          <Route
            path="/identity/login-sso-google"
            element={isAuthenticated ? <Navigate to="/" replace /> : <LoginSsoGooglePage />}
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

const App: React.FC = () => {
  const { isExecuting } = useCurrentUser();

  if (isExecuting) {
    return (
      <div className="flex justify-center items-center min-h-screen">
        <div className="animate-spin rounded-full h-10 w-10 border-4 border-gray-300 border-t-blue-500">Loading...</div>
      </div>
    );
  }

  return (
    <Router>
      <AppContent />
    </Router>
  );
};

export default App;
