import { UsageConstants } from '../constants';
import { recorder } from '../recorder';
import { client as apiHost1 } from './apiHost1/client.gen';
import { logout, ProblemDetails, refreshToken } from './websiteHost';
import { client as websiteHost } from './websiteHost/client.gen';


const unRetryableRequestUrls: string[] = ['/api/auth/refresh', '/api/auth'];
export const homePath = '/';

// This function sets up the appropriate request headers and handlers,
// as detailed in docs/design-principles/0110-back-end-for-front-end.md
function initializeApiClient() {
  const csrfToken = document.querySelector("meta[name='csrf-token']")?.getAttribute('content');

  const noIndexedArraysQuerySerializer = (params: Record<string, unknown>) => {
    const searchParams = new URLSearchParams();

    Object.entries(params).forEach(([key, value]) => {
      if (value === undefined || value === null) return;

      if (Array.isArray(value)) {
        // No indexes - just repeat the key for each value
        value.forEach(v => searchParams.append(key, String(v)));
      } else {
        searchParams.append(key, String(value));
      }
    });

    return searchParams.toString();
  };

  apiHost1.setConfig({
    baseUrl: `${import.meta.env.VITE_WEBSITEHOSTBASEURL}/api`,
    headers: {
      accept: 'application/json',
      'content-type': 'application/json',
      'anti-csrf-tok': csrfToken
    },
    throwOnError: false,
    querySerializer: noIndexedArraysQuerySerializer
  });
  websiteHost.setConfig({
    baseUrl: `${import.meta.env.VITE_WEBSITEHOSTBASEURL}`,
    headers: {
      accept: 'application/json',
      'content-type': 'application/json',
      'anti-csrf-tok': csrfToken
    },
    throwOnError: false,
    querySerializer: noIndexedArraysQuerySerializer
  });

  apiHost1.interceptors.response.use((response, request) => handleUnauthorizedResponse(response, request));
  websiteHost.interceptors.response.use((response, request) => handleUnauthorizedResponse(response, request));
}

// This handles refreshing access tokens when any request returns a 401,
// as detailed in docs/design-principles/0110-back-end-for-front-end.md
// Note: we should only reject the original response to halt processing for some reason,
// otherwise resolve the response continues the chain.
// Note: if after refreshing the token, and performing the original request again,
// we should also resolve the retried response, effectively swapping the response.
// If we reject the original response, then that response is thrown and caught in the useActionCommand or useActionQuery hooks catch handler.
// Note: This interceptor is called by all generated client methods.
// We are deliberately calling refreshToken() (a generated client method), which, if fails,
// will call this interceptor a second time around with a 401,
// and we are intentionally ignoring that error (as an unRetryableRequestUrls),
// so that we can handle the response fully in the then() clause (below) of the first interceptor call.
async function handleUnauthorizedResponse(response: Response, request: Request): Promise<Response> {
  if (!response) {
    return Promise.resolve(response);
  }

  if (response.ok) {
    return Promise.resolve(response);
  }

  const error = await getResponseBody(response);

  //Handle 403's for CSRF
  const problem = error as ProblemDetails;
  if (response.status === 403 && problem != undefined && problem.title === 'csrf_violation') {
    recorder.traceDebug('UnAuthorizedHandler: CSRF violation detected, reloading home page');
    forceReloadHome();
    return Promise.resolve(response); // should never get here, this should bypass this error altogether
  }

  // Only handle 401s
  if (response.status !== 401) {
    return Promise.resolve(response);
  }

  // We don't want to retry any of these API calls
  const path = new URL(request.url).pathname;
  if (unRetryableRequestUrls.includes(path)) {
    return Promise.resolve(response);
  }

  recorder.traceDebug("UnAuthorizedHandler: 401 detected, refreshing user's token");
  // Attempt to refresh the access_token cookies (if exist)
  // This request will call back to this interceptor, but will be short-circuited by the unRetryableRequestUrls check above
  return await refreshToken().then(async res => {
    if (res.response.ok) {
      recorder.traceDebug("UnAuthorizedHandler: Refreshed user's token, retrying original request");
      recorder.trackUsage(UsageConstants.UsageScenarios.BrowserAuthRefresh);
      // Retry the original request (using fetch directly, not client)
      const retry = await fetch(request.url, {
        method: request.method,
        headers: request.headers,
        body: request.method === 'GET' || request.method === 'HEAD' ? undefined : request.body,
        credentials: 'include'
      });
      if (retry.ok) {
        // Retrying original request now succeeds
        return Promise.resolve(retry);
      } else {
        if (retry.status === 401) {
          recorder.traceDebug('UnAuthorizedHandler: Original request still returns 401, forcing logout');
          // Assume, the user is no-longer authenticated anymore. Best we can do here is force the user to login again
          logout().then(() => forceReloadHome());
          return Promise.resolve(response); // should never get here, this should bypass this error altogether
        } else {
          recorder.traceDebug('UnAuthorizedHandler: Original request failed with: {Error}', { error: retry.status });
          return Promise.resolve(retry); // This response will be different from the one passed into this interceptor.
        }
      }
    } else {
      if (res.response.status === 423 || res.response.status === 401) {
        recorder.traceDebug("UnAuthorizedHandler: Refreshing user's token returned 423, forcing logout");
        // Access token does not exist, or Refresh token is expired, or User is locked and cannot be refreshed,
        // or they cannot be authenticated anymore, the best we can do here is force the user to log out,
        // remove all cookies, and force them to login again.
        logout().then(() => forceReloadHome());
        return Promise.resolve(response); // should never get here, this should bypass this error altogether
      } else {
        recorder.traceDebug("UnAuthorizedHandler: Refreshing user's token failed with: {Error}", {
          error: res.response.status
        });
        return Promise.resolve(res.response); // This response will be different from the one passed into this interceptor.
      }
    }
  });
}

// Send the user home, re-fetching index.html, and refreshing CSRF token and auth cookies
function forceReloadHome() {
  window.location.assign(homePath);
}

async function getResponseBody(response: Response) {
  const responseClone = response.clone();
  const textBody = await responseClone.text();
  let jsonBody: unknown = undefined;
  try {
    jsonBody = JSON.parse(textBody);
  } catch {
    //noop
  }
  return jsonBody ?? textBody;
}

export { initializeApiClient };
