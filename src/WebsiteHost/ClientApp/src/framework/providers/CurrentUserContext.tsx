import { createContext, ReactNode, useContext, useEffect } from 'react';
import { LogoutAction } from '../../subDomains/identity/actions/logout.ts';
import { GetProfileForCallerAction } from '../../subDomains/userProfiles/actions/getProfileForCaller.ts';
import { UserProfileForCaller } from '../api/apiHost1';
import { anonymousUser } from '../constants.ts';


interface CurrentUserProviderProps {
  children?: ReactNode;
}

interface CurrentUserContextContent {
  profile: UserProfileForCaller;
  isSuccess?: boolean;
  isExecuting: boolean;
  isAuthenticated: boolean;
}

const CurrentUserContext = createContext<CurrentUserContextContent | null>({
  profile: anonymousUser,
  isSuccess: false,
  isExecuting: false,
  isAuthenticated: false
});

export const CurrentUserProvider = ({ children }: CurrentUserProviderProps) => {
  const {
    execute: getCallerProfile,
    lastSuccessResponse: callerProfile,
    isSuccess: isProfileSuccess,
    isExecuting: isProfileExecuting
  } = GetProfileForCallerAction();

  const { execute: logout, isExecuting: isLoggingOut, isSuccess: isLogoutSuccess } = LogoutAction();

  useEffect(() => getCallerProfile(), []);

  // If we have an error fetching the current user profile,
  // and we're not already logging out, then log out now to remove any authenticated state (i.e. cookies).
  useEffect(() => {
    if (isProfileSuccess == false) {
      if (isLogoutSuccess === undefined && !isLoggingOut) {
        logout();
      }
    }
  }, [logout, isLoggingOut, isLogoutSuccess, isProfileSuccess]);

  const profile = callerProfile ?? anonymousUser;

  return (
    <CurrentUserContext.Provider
      value={{
        profile,
        isSuccess: isProfileSuccess,
        isExecuting: isProfileExecuting,
        isAuthenticated: profile.isAuthenticated
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
    isSuccess: context.isSuccess,
    isExecuting: context.isExecuting,
    isAuthenticated: context.isAuthenticated
  };
};
