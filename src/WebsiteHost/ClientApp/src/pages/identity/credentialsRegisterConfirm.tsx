import React from 'react';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';

// Creates a confirmation page for the user to confirm their credentials registration
// Accepts a "token" in the query string, from a user clicking on a link in an email.
// Sends the token to the API to confirm the registration, and process the response:
// 1. If the token is valid, and the user has not yet registered, then the user is redirected to the login page.
// 2. If the token is valid, but the user has already registered, then the user is shown an error message.
// 3. The token is invalid (perhaps expired, or unknown) then the user is shown an error message, and a link to resend the confirmation email.
export const CredentialsRegisterConfirm: React.FC = () => {
  const { t: translate } = useTranslation('common');

  return (
    <div className="container min-h-screen flex">
      <div className="rounded-2xl shadow-2xl p-8 bg-white lg:w-3/5 md:w-3/5 w-11/12">
        <h1 className="text-4xl font-bold text-center mb-16">
          {translate('pages.identity.credentials_register_confirm.title')}
        </h1>

        <div className="text-center">
          <Link to="/" className="btn btn-secondary">
            {translate('pages.identity.credentials_register_confirm.links.home')}
          </Link>
        </div>
      </div>
    </div>
  );
};
