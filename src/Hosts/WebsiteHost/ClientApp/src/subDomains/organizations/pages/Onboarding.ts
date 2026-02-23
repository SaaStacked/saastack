import {
  Organization,
  OrganizationOnboardingStatus,
  OrganizationOnboardingStepSchemaType,
  OrganizationOnboardingWorkflowSchema,
  OrganizationOwnership
} from '../../../framework/api/apiHost1';


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

export const customWorkflow = (): OrganizationOnboardingWorkflowSchema => ({
  name: 'AllUsers',
  startStepId: 'start',
  endStepId: 'end',
  steps: {
    start: {
      id: 'start',
      title: 'start',
      description: 'Start of the onboarding journey',
      type: OrganizationOnboardingStepSchemaType.START,
      weight: 40,
      nextStepId: 'setup',
      branches: [],
      initialValues: {}
    },
    setup: {
      id: 'setup',
      title: 'setup',
      description: 'A middle step',
      type: OrganizationOnboardingStepSchemaType.NORMAL,
      weight: 60,
      nextStepId: 'end',
      branches: [],
      initialValues: {}
    },
    end: {
      id: 'end',
      title: 'end',
      description: 'The last step',
      type: OrganizationOnboardingStepSchemaType.END,
      weight: 0,
      branches: [],
      initialValues: {}
    }
  }
});
