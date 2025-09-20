import { UserProfileClassification, UserProfileForCaller } from '../api/apiHost1';


export const anonymousUserId = 'xxx_anonymous0000000000000';
export const anonymousUser = {
  id: anonymousUserId,
  userId: anonymousUserId,
  isAuthenticated: false,
  address: {
    countryCode: 'USA'
  },
  avatarUrl: null,
  classification: 'Person' as UserProfileClassification,
  displayName: 'anonymous',
  emailAddress: null,
  name: {
    firstName: 'anonymous'
  },
  phoneNumber: null,
  timezone: null,
  defaultOrganizationId: null,
  features: [],
  roles: []
} as unknown as UserProfileForCaller;

export namespace UsageConstants {
  export enum UsageScenarios {
    BrowserAuthRefresh = 'Browser Authentication Refreshed'
    //EXTEND: add new events here, refer to the UsageConstants.cs file for more usage scenarios
  }

  export enum Properties {
    ForId = 'ForId',
    Id = 'ResourceId',
    ResourceId = 'ResourceId',
    TenantId = 'TenantId'
    //EXTEND: add new properties here, try to align with those in  UsageConstants.cs file
  }
}
