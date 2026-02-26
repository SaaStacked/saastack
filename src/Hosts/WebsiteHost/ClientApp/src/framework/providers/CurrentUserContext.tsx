import { createContext, ReactNode, useContext, useEffect } from 'react';
import { LogoutAction } from '../../subDomains/identity/actions/logout.ts';
import {
  GetOrganizationAction,
  OrganizationErrorCodes
} from '../../subDomains/organizations/actions/getOrganization.ts';
import { GetProfileForCallerAction } from '../../subDomains/userProfiles/actions/getProfileForCaller.ts';
import { GetOrganizationData, Organization, UserProfileForCaller } from '../api/apiHost1';
import { refreshToken } from '../api/websiteHost';
import { anonymousUser } from '../constants.ts';
import { recorder } from '../recorder.ts';

interface CurrentUserProviderProps {
  children?: ReactNode;
}

interface CurrentUserContextContent {
  profile: UserProfileForCaller;
  organization?: Organization;
  isSuccess?: boolean;
  isExecuting: boolean;
  isAuthenticated: boolean;
  refetch: () => void;
}

const CurrentUserContext = createContext<CurrentUserContextContent | null>({
  profile: anonymousUser,
  organization: undefined,
  isSuccess: false,
  isExecuting: false,
  isAuthenticated: false,
  refetch(): void {}
});

export const CurrentUserProvider = ({ children }: CurrentUserProviderProps) => {
  const {
    execute: getCallerProfile,
    lastSuccessResponse: callerProfile,
    isSuccess: isProfileSuccess,
    isExecuting: isProfileExecuting
  } = GetProfileForCallerAction();
  const {
    execute: getOrganization,
    lastSuccessResponse: organization,
    isSuccess: isOrganizationSuccess,
    isExecuting: isOrganizationExecuting,
    lastExpectedError: organizationExpectedError
  } = GetOrganizationAction();

  const { execute: logout, isExecuting: isLoggingOut, isSuccess: isLogoutSuccess } = LogoutAction();

  useEffect(() => getCallerProfile(), []);

  // If we have a default organization, then fetch it
  useEffect(() => {
    if (profileHasDefaultOrganization()) {
      getOrganization({ path: { Id: callerProfile!.defaultOrganizationId } } as GetOrganizationData);
      // Handle temporary 403 error on organization fetch - may occur after SSO authentication for the first time
      if (organizationExpectedError?.code === OrganizationErrorCodes.forbidden) {
        recorder.traceDebug('CurrentUserProvider: 403 on default organization fetch, refreshing token and retrying');
        setTimeout(
          () =>
            refreshToken().then(async res => {
              if (res.response.ok) {
                recorder.traceDebug('CurrentUserProvider: Token refreshed, retrying organization fetch');
                getOrganization({ path: { Id: callerProfile!.defaultOrganizationId } } as GetOrganizationData);
              }
            }),
          2000
        );
      }
    }
  }, [callerProfile?.defaultOrganizationId, isProfileSuccess, organizationExpectedError]);

  // If we have an error fetching the current user profile,
  // and we're not already logging out, then log out now to remove any authenticated state (i.e. cookies).
  useEffect(() => {
    if (isProfileSuccess == false) {
      if (isLogoutSuccess === undefined && !isLoggingOut) {
        logout();
      }
    }
  }, [logout, isLoggingOut, isLogoutSuccess, isProfileSuccess]);

  const profileHasDefaultOrganization = (): boolean =>
    isProfileSuccess === true && callerProfile != undefined && callerProfile!.defaultOrganizationId != undefined;
  const refetchAll = () =>
    getCallerProfile(
      {},
      {
        onSuccess: ({ response }) => {
          if (response?.defaultOrganizationId) {
            getOrganization({ path: { Id: response.defaultOrganizationId } } as GetOrganizationData);
          }
        }
      }
    );

  const isSuccess = profileHasDefaultOrganization()
    ? isProfileSuccess === undefined
      ? undefined
      : isProfileSuccess && isOrganizationSuccess === true
    : isProfileSuccess;

  const isExecuting = profileHasDefaultOrganization()
    ? isProfileExecuting || isOrganizationExecuting
    : isProfileExecuting;
  const profile = (callerProfile ?? anonymousUser) as UserProfileForCaller;
  return (
    <CurrentUserContext.Provider
      value={{
        profile,
        organization,
        isSuccess,
        isExecuting,
        isAuthenticated: profile.isAuthenticated,
        refetch: refetchAll
      }}
    >
      {children}
    </CurrentUserContext.Provider>
  );
};

export const useCurrentUser = () => {
  const context = useContext(CurrentUserContext);

  if (!context) {
    throw new Error('useCurrentUser must be used within CurrentUserProvider');
  }

  return {
    profile: context.profile,
    organization: context.organization,
    isSuccess: context.isSuccess,
    isExecuting: context.isExecuting,
    isAuthenticated: context.isAuthenticated,
    refetch: context.refetch
  };
};
