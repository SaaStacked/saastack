import React from 'react';
import { useTranslation } from 'react-i18next';
import { useLocation, useNavigate } from 'react-router-dom';
import { Organization } from '../../../framework/api/apiHost1';
import Icon from '../../../framework/components/icon/Icon.tsx';
import { RoutePaths } from '../../../framework/constants.ts';
import { useCurrentUser } from '../../../framework/providers/CurrentUserContext.tsx';
import { shouldBeOnboarding } from '../pages/Onboarding.ts';


// Creates a popup reminder that onboarding is in-progress,
// and prompts the user to complete that process
export const OnboardingReminder: React.FC = () => {
  const { t: translate } = useTranslation();
  const navigate = useNavigate();
  const location = useLocation();
  const { organization } = useCurrentUser();

  if (!shouldShowReminder(location.pathname, organization)) {
    return null;
  }

  const navigateToOnboarding = () => navigate(RoutePaths.OrganizationOnboarding);

  return (
    <div
      className="fixed bottom-6 right-6 bg-brand-primary-600 text-white rounded-lg shadow-lg p-4 cursor-pointer hover:bg-brand-primary-700 transition-colors max-w-sm z-50"
      id="onboarding_reminder"
      data-testid="onboarding_reminder"
      onClick={navigateToOnboarding}
      role="button"
      tabIndex={0}
      onKeyDown={(e) => {
        if (e.key === 'Enter' || e.key === ' ') {
          navigateToOnboarding();
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

function shouldShowReminder(pathname: string, organization?: Organization) {
  return shouldBeOnboarding(organization) && pathname !== RoutePaths.OrganizationOnboarding;
}
