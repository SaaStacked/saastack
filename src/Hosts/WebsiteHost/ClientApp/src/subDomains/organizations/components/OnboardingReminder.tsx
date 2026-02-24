import React from 'react';
import { useTranslation } from 'react-i18next';
import { useNavigate } from 'react-router-dom';
import Icon from '../../../framework/components/icon/Icon.tsx';
import { RoutePaths } from '../../../framework/constants.ts';
import { useCurrentUser } from '../../../framework/providers/CurrentUserContext.tsx';
import { shouldShowOnboardingReminder } from '../pages/Onboarding.ts';

// Creates a popup reminder that onboarding is in progress, and prompts the user to complete tasks within it
export const OnboardingReminder: React.FC = () => {
  const { t: translate } = useTranslation();
  const navigate = useNavigate();
  const { organization } = useCurrentUser();

  if (!shouldShowOnboardingReminder(organization)) {
    return null;
  }

  const handleClick = () => navigate(RoutePaths.OrganizationOnboarding);

  return (
    <div
      onClick={handleClick}
      className="fixed bottom-6 right-6 bg-brand-primary-600 text-white rounded-lg shadow-lg p-4 cursor-pointer hover:bg-brand-primary-700 transition-colors max-w-sm z-50"
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          handleClick();
        }
      }}
    >
      <div className="flex items-start gap-3">
        <div className="flex-shrink-0">
          <Icon symbol="check-circle-fill" className="w-6 h-6" />
        </div>
        <div className="flex-1">
          <h3 className="font-semibold mb-1">{translate('pages.organizations.onboarding.reminder.title')}</h3>
          <p className="text-sm opacity-90">{translate('pages.organizations.onboarding.reminder.message')}</p>
        </div>
      </div>
    </div>
  );
};
