import { createContext, ReactNode, useContext, useEffect } from 'react';
import { UserProfileForCaller } from '../../api/apiHost1';
import { anonymousUser } from '../../constants.ts';
import { GetProfileForCallerAction } from '../userProfiles/getProfileForCaller.tsx';
import { LogoutAction } from './logoutUser.tsx';


interface CurrentUserProviderProps {
  children?: ReactNode;
}

interface CurrentUserContextContent {
  profile: UserProfileForCaller;
  isSuccess?: boolean;
  isAuthenticated: boolean;
}

const CurrentUserContext = createContext<CurrentUserContextContent | null>({
  profile: anonymousUser,
  isSuccess: false,
  isAuthenticated: false
});

export const CurrentUserProvider = ({ children }: CurrentUserProviderProps) => {
  const {
    execute: getCallerProfile,
    lastSuccessResponse: callerProfile,
    isSuccess: isProfileSuccess
  } = GetProfileForCallerAction();

  const { execute: logout, isExecuting: isLoggingOut, isSuccess: isLogoutSuccess } = LogoutAction();

  useEffect(() => getCallerProfile(), []);

  // If we have an error fetching the profile, and we're not already logging out, log out.
  useEffect(() => {
    if (!isProfileSuccess && isLogoutSuccess === undefined && !isLoggingOut) {
      logout({});
    }
  }, [logout, isLoggingOut, isLogoutSuccess, isProfileSuccess]);

  const profile = callerProfile ?? anonymousUser;

  return (
    <CurrentUserContext.Provider
      value={{ profile, isSuccess: isProfileSuccess, isAuthenticated: profile.isAuthenticated }}
    >
      {children}
    </CurrentUserContext.Provider>
  );
};

export function useCurrentUser(): CurrentUserContextContent {
  const context = useContext(CurrentUserContext);

  if (!context) {
    throw new Error('useCurrentUser must be used within CurrentUserProvider');
  }

  return {
    profile: context.profile,
    isSuccess: context.isSuccess,
    isAuthenticated: context.isAuthenticated
  };
}
