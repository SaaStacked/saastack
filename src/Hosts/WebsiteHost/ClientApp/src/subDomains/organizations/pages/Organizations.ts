// Keep up to date with Domain.Interfaces.Authorization.TenantRoles
export enum TenantRoles {
  Member = 'tnt_mem',
  Owner = 'tnt_own',
  BillingAdmin = 'tnt_bill_adm'
}

// Keep up to date with Domain.Interfaces.Authorization.TenantFeatures
export enum TenantFeatures {
  Basic = 'tnt_basic',
  PaidTrial = 'tnt_paidtrial',
  Paid2 = 'tnt_paid2',
  Paid3 = 'tnt_paid3'
}

export const formatRoleName = (translate: (key: string) => string, role: string) =>
  translate(`pages.organizations.labels.roles.${role}`);

export const formatFeatureName = (translate: (key: string) => string, feature: string) =>
  translate(`pages.organizations.labels.features.${feature}`);
