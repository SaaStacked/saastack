// Keep up to date with Domain.Interfaces.Authorization.PlatformRoles
export enum PlatformRoles {
  Standard = 'plt_std',
  Operations = 'plt_ops'
}

// Keep up to date with Domain.Interfaces.Authorization.PlatformFeatures
export enum PlatformFeatures {
  Basic = 'plt_basic',
  PaidTrial = 'plt_paidtrial',
  Paid2 = 'plt_paid2',
  Paid3 = 'plt_paid3'
}

export const formatRoleName = (translate: (key: string) => string, role: string) =>
  translate(`pages.profiles.labels.roles.${role}`);

export const formatFeatureName = (translate: (key: string) => string, feature: string) =>
  translate(`pages.profiles.labels.features.${feature}`);
