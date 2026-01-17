import React, { useEffect, useRef } from 'react';
import { useTranslation } from 'react-i18next';
import { useSearchParams } from 'react-router-dom';
import { EmptyRequest } from '../../../framework/api/EmptyRequest.ts';
import Alert from '../../../framework/components/alert/Alert.tsx';
import ButtonAction from '../../../framework/components/button/ButtonAction.tsx';
import FormAction from '../../../framework/components/form/FormAction.tsx';
import FormPage from '../../../framework/components/form/FormPage.tsx';
import FormSubmitButton from '../../../framework/components/form/formSubmitButton/FormSubmitButton.tsx';
import Icon from '../../../framework/components/icon/Icon.tsx';
import PageAction, { PageActionRef } from '../../../framework/components/page/PageAction.tsx';
import { ConsentOAuth2ClientAction } from '../actions/consentOAuth2Client.ts';
import { GetOAuth2ClientConsentAction } from '../actions/GetOAuth2ClientConsent.ts';

export const OAuth2ConsentClientPage: React.FC = () => {
  const { t: translate } = useTranslation();
  const [searchParams] = useSearchParams();
  const clientId = searchParams.get('client_id');
  const redirectUri = searchParams.get('redirect_uri');
  const scope = searchParams.get('scope');
  const state = searchParams.get('state');
  const consentStatus = GetOAuth2ClientConsentAction(clientId ?? '', scope ?? '');
  const consentStatusTrigger = useRef<PageActionRef<EmptyRequest>>(null);
  const acceptClientConsent = ConsentOAuth2ClientAction(clientId ?? '', redirectUri ?? '', scope ?? '', state, true);
  const denyClientConsent = ConsentOAuth2ClientAction(clientId ?? '', redirectUri ?? '', scope ?? '', state, false);

  useEffect(() => consentStatusTrigger.current?.execute(), []);

  const scopes = scope?.split(' ') || [];
  const scopeDescriptions = {
    openid: translate('pages.identity.oauth2_consent_client.labels.scopes.openid'),
    profile: translate('pages.identity.oauth2_consent_client.labels.scopes.profile'),
    email: translate('pages.identity.oauth2_consent_client.labels.scopes.email')
  };

  return (
    <FormPage width="default">
      <PageAction
        id="oauth2_get_consent_status"
        action={consentStatus}
        ref={consentStatusTrigger}
        loadingMessage={translate('pages.identity.oauth2_consent_client.loader')}
      >
        <div className="max-w-md mx-auto bg-white dark:bg-neutral-800 rounded-lg shadow-lg p-6">
          <div className="text-center mb-6">
            <div className="w-16 h-16 mx-auto mb-4 bg-blue-100 dark:bg-blue-900 rounded-full flex items-center justify-center">
              <Icon symbol="shield" size={32} color="brand-primary" />
            </div>
            <h2 className="text-xl font-semibold text-neutral-900 dark:text-white mb-2">
              {translate('pages.identity.oauth2_consent_client.header.title')}
            </h2>
            <p className="text-sm text-neutral-600 dark:text-neutral-400">
              {translate('pages.identity.oauth2_consent_client.header.subtitle', {
                appName: consentStatus.lastSuccessResponse?.client.name || clientId
              })}
            </p>
          </div>

          <div className="mb-6">
            <h3 className="text-sm font-medium text-neutral-900 dark:text-white mb-3">
              {translate('pages.identity.oauth2_consent_client.labels.permissions')}
            </h3>
            <div className="space-y-2">
              {scopes.map((scopeItem) => (
                <div key={scopeItem} className="flex items-start space-x-3">
                  <Icon symbol="check" size={16} color="brand-secondary" className="mt-0.5 flex-shrink-0" />
                  <div>
                    <p className="text-sm text-neutral-900 dark:text-white font-medium">
                      {scopeDescriptions[scopeItem as keyof typeof scopeDescriptions] || scopeItem}
                    </p>
                  </div>
                </div>
              ))}
            </div>
          </div>

          <Alert
            id="security_notice"
            type="info"
            title={translate('pages.identity.oauth2_consent_client.labels.security.title')}
            message={translate('pages.identity.oauth2_consent_client.labels.security.message')}
          />

          <FormAction id="consent_client" action={acceptClientConsent}>
            <div className="grid grid-cols-2 gap-3">
              <div className="w-full mt-4">
                <ButtonAction
                  className="w-full"
                  id="deny"
                  action={denyClientConsent}
                  variant="danger"
                  label={translate('pages.identity.oauth2_consent_client.actions.deny')}
                />
              </div>
              <div className="w-full mt-0">
                <FormSubmitButton label={translate('pages.identity.oauth2_consent_client.actions.authorize')} />
              </div>
            </div>
          </FormAction>

          <div className="mt-6 pt-4 border-t border-neutral-200 dark:border-neutral-700">
            <p className="text-xs text-neutral-500 dark:text-neutral-400 text-center">
              {translate('pages.identity.oauth2_consent_client.labels.privacy_notice')}
            </p>
          </div>
        </div>
      </PageAction>
    </FormPage>
  );
};
