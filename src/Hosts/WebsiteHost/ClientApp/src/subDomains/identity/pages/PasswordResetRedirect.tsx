import React from 'react';
import { useTranslation } from 'react-i18next';
import { Link, useSearchParams } from 'react-router-dom';
import ButtonAction from '../../../framework/components/button/ButtonAction.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import Icon from '../../../framework/components/icon/Icon.tsx';
import { RoutePaths } from '../../../framework/constants.ts';
import { ResendPasswordResetAction } from '../actions/resendPasswordReset.ts';


export const PasswordResetRedirectPage: React.FC = () => {
  const { t: translate } = useTranslation();
  const [queryString] = useSearchParams();
  const token = queryString.get('token');
  const resend = ResendPasswordResetAction();

  return (
    <FormPage title={translate('pages.identity.credentials_password_reset_redirect.title')}>
      <h2 className="text-2xl font-bold text-center mb-8">
        {translate('pages.identity.credentials_password_reset_redirect.confirmation_message.title')}
      </h2>
      <p>{translate('pages.identity.credentials_password_reset_redirect.confirmation_message.message')}</p>
      <div className="flex justify-center">
        <Icon symbol="email" size={96} color="brand-secondary" />
      </div>
      <h3 className="text-2xl font-bold text-center mt-4">
        {translate('pages.identity.credentials_password_reset_redirect.instructions.title')}:
      </h3>
      <div className="prose text-sm text-left">
        <ul>
          <li>{translate('pages.identity.credentials_password_reset_redirect.instructions.steps.step1')}</li>
          <li>{translate('pages.identity.credentials_password_reset_redirect.instructions.steps.step2')}</li>
          <li>{translate('pages.identity.credentials_password_reset_redirect.instructions.steps.step3')}</li>
        </ul>

        <h3>{translate('pages.identity.credentials_password_reset_redirect.troubleshoot.question')}</h3>
        <p>{translate('pages.identity.credentials_password_reset_redirect.troubleshoot.answer')}</p>
      </div>
      {token && (
        <ButtonAction
          className="mt-4 p-2 rounded-full w-8 h-8"
          id="resend"
          busyLabel={translate('pages.identity.credentials_password_reset_initiate.resending.loader')}
          action={resend}
          requestData={{ token }}
          variant="brand-secondary"
        >
          <Icon symbol="repeat" size={16} color="white" />
          <span className="pl-2">
            {translate('pages.identity.credentials_password_reset_initiate.resending.title')}
          </span>
        </ButtonAction>
      )}
      <div className="text-center">
        <Link to={RoutePaths.Home}>{translate('pages.identity.credentials_password_reset_redirect.links.home')}</Link>
      </div>
    </FormPage>
  );
};
