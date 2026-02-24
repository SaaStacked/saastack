import { useLocation } from 'react-router-dom';
import {
  Organization,
  OrganizationOnboardingStatus,
  OrganizationOnboardingStepSchemaType,
  OrganizationOnboardingWorkflowSchema,
  OrganizationOwnership
} from '../../../framework/api/apiHost1';
import { RoutePaths } from '../../../framework/constants.ts';

// Should be onboarding if:
// 1. Organization exists
// 2. Organization is Personal
// 3. Onboarding is not yet completed
export const shouldBeOnboarding = (organization?: Organization) =>
  organization &&
  organization.ownership === OrganizationOwnership.PERSONAL &&
  organization.onboardingStatus !== OrganizationOnboardingStatus.COMPLETE;

export const shouldInitiateOnboarding = (organization?: Organization) =>
  shouldBeOnboarding(organization) && organization!.onboardingStatus === OrganizationOnboardingStatus.NOT_STARTED;

// Show reminder if:
// 1. Onboarding is InProgress
// 4. User is viewing some other route than the onboarding page
export const shouldShowOnboardingReminder = (organization?: Organization) => {
  const location = useLocation();

  return shouldBeOnboarding(organization) && location.pathname !== RoutePaths.OrganizationOnboarding;
};

export const customWorkflow = (): OrganizationOnboardingWorkflowSchema => ({
  name: 'Getting Started',
  startStepId: 'welcome',
  endStepId: 'complete',
  steps: {
    welcome: {
      id: 'welcome',
      title: 'Welcome',
      description: 'Welcome to your onboarding journey',
      type: OrganizationOnboardingStepSchemaType.START,
      weight: 40,
      nextStepId: 'setup',
      branches: [],
      initialValues: {}
    },
    setup: {
      id: 'setup',
      title: 'Setup',
      description: 'Configure your preferences',
      type: OrganizationOnboardingStepSchemaType.NORMAL,
      weight: 60,
      nextStepId: 'complete',
      branches: [],
      initialValues: {}
    },
    complete: {
      id: 'complete',
      title: 'That&apos;s it!',
      description: 'Click the Finish button and start using the product!',
      type: OrganizationOnboardingStepSchemaType.END,
      weight: 0,
      branches: [],
      initialValues: {}
    }
  }
});
