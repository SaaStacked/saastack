import { UserProfileClassification, UserProfileForCaller } from './api/apiHost1';

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

export namespace LocalStorageKeys {
  export const Theme = 'theme';
  export const IsPendingOAuth2Authorization = 'isPendingOAuth2Authorization';
}

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

// EXTEND: Keep these update to date with paths defined in WebsiteUiService
export namespace RoutePaths {
  export const Home = '/';
  export const About = '/about';
  export const Terms = '/terms';
  export const Privacy = '/privacy';
  export const CredentialsLogin = '/identity/credentials/login';
  export const SsoMicrosoft = '/identity/sso/microsoft';
  export const SsoGoogle = '/identity/sso/google';
  export const OAuth2Authorize = '/identity/oauth2/authorize';
  export const OAuth2AuthorizeAlt = '/identity/oauth2/auth';
  export const OAuth2ConsentClient = '/identity/oauth2/client/consent';
  export const PasswordReset = '/identity/credentials/password-reset';
  export const PasswordResetRedirect = '/identity/credentials/password-reset-redirect';
  export const PasswordResetComplete = '/identity/credentials/password-reset-complete';
  export const RegisterRedirect = '/identity/credentials/register-redirect';
  export const RegisterConfirm = '/identity/credentials/register-confirm';
  export const Register = '/identity/credentials/register';
  export const MfaOobConfirm = '/identity/credentials/2fa/mfaoob-confirm';
  export const UserProfile = '/profile';
  export const Organizations = '/organizations';
  export const OrganizationsNew = '/organizations/new';
  export const OrganizationsEdit = '/organizations/:id';
}
