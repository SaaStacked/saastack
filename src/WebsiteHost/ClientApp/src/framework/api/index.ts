import axios, { AxiosError, HttpStatusCode } from 'axios';
import { UsageConstants } from '../constants';
import { recorder } from '../recorder';
import { client as apiHost1 } from './apiHost1/services.gen';
import { logout, ProblemDetails } from './websiteHost';
import { refreshToken, client as websiteHost } from './websiteHost/services.gen';


const unRetryableRequestUrls: string[] = ['/api/auth/refresh', '/api/auth'];
export const homePath = '/';

// This function sets up the appropriate request headers and handlers,
// as detailed in docs/design-principles/0110-back-end-for-front-end.md
function initializeApiClient() {
  const csrfToken = document.querySelector("meta[name='csrf-token']")?.getAttribute('content');
  apiHost1.setConfig({
    baseURL: `${import.meta.env.VITE_WEBSITEHOSTBASEURL}/api`,
    headers: {
      accept: 'application/json',
      'content-type': 'application/json',
      'anti-csrf-tok': csrfToken
    },
    paramsSerializer: {
      indexes: null // To prevent axios from encoding array indexes in the query string
    }
  });
  websiteHost.setConfig({
    baseURL: `${import.meta.env.VITE_WEBSITEHOSTBASEURL}`,
    headers: {
      accept: 'application/json',
      'content-type': 'application/json',
      'anti-csrf-tok': csrfToken
    },
    paramsSerializer: {
      indexes: null // To prevent axios from encoding array indexes in the query string
    }
  });

  apiHost1.instance.interceptors.response.use((res) => res, handleUnauthorizedResponse);
  websiteHost.instance.interceptors.response.use((res) => res, handleUnauthorizedResponse);
}

// This handles refreshing access tokens when any request returns a 401,
// as detailed in docs/design-principles/0110-back-end-for-front-end.md
async function handleUnauthorizedResponse(error: AxiosError) {
  const requestConfig = error.config;

  //Handle 403's for CSRF
  const problem = error as AxiosError<ProblemDetails>;
  if (
    error.status === HttpStatusCode.Forbidden &&
    problem != undefined &&
    problem.response?.data.title === 'csrf_violation'
  ) {
    recorder.traceDebug('UnAuthorizedHandler: CSRF violation detected, reloading home page');
    forceReloadHome();
    return Promise.reject(error);
  }

  // Only handle 401s
  if (error.status !== HttpStatusCode.Unauthorized) {
    return Promise.reject(error);
  }

  // Check it is an axios response (i.e. has config)
  if (!requestConfig) {
    return Promise.reject(error);
  }

  // We don't want to retry any of these API calls
  if (unRetryableRequestUrls.includes(requestConfig.url!)) {
    return Promise.reject(error);
  }

  try {
    recorder.traceDebug("UnAuthorizedHandler: 401 detected, refreshing user's token");
    // Attempt to refresh the access_token cookies (if exist)
    return await refreshToken().then(async (res) => {
      if (axios.isAxiosError(res)) {
        const error = res as AxiosError;
        if (error.status === HttpStatusCode.Unauthorized || error.status === HttpStatusCode.Locked) {
          recorder.traceDebug("UnAuthorizedHandler: Refreshing user's token returned 401, forcing logout");
          // Access token does not exist, or Refresh token is expired, or User is locked and cannot be refreshed,
          // or they cannot be authenticated anymore, the best we can do here is force the user to log out,
          // remove all cookies, and force them to login again.
          logout().then(() => forceReloadHome());
        }
        recorder.traceDebug("UnAuthorizedHandler: Refreshing user's token failed with error:", { error });
        return Promise.reject(error);
      } else {
        recorder.traceDebug("UnAuthorizedHandler: Refreshed user's token, retrying original request");
        recorder.trackUsage(UsageConstants.UsageScenarios.BrowserAuthRefresh);
        // Retry the original request
        return axios.request(requestConfig).then((res) => {
          if (axios.isAxiosError(res)) {
            const error = res as AxiosError;
            if (error.status === HttpStatusCode.Unauthorized) {
              recorder.traceDebug('UnAuthorizedHandler: Original request still returns 401, forcing logout');
              // User is not authenticated anymore, the best we can do here is force the user to login again
              forceReloadHome();
              return Promise.reject(error);
            }
            recorder.traceDebug('UnAuthorizedHandler: Original request failed with error', { error });
            return Promise.reject(error);
          } else {
            return res;
          }
        });
      }
    });
  } catch (error) {
    return Promise.reject(error);
  }
}

// Send the user home, by re-fetching index.html, and refreshing CSRF token and cookie
function forceReloadHome() {
  window.location.assign(homePath);
}

export { initializeApiClient };
