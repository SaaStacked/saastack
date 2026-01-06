import { beforeEach, describe, expect, it, vi } from 'vitest';
import { RoutePaths } from '../constants';
import { EmptyResponse, ProblemDetails } from './apiHost1';
import { client as apiHost1 } from './apiHost1/client.gen';
import { initializeApiClient } from './index';
import { logout, refreshToken } from './websiteHost';


type ResInterceptor<Res, Req, Options> = (response: Res, request: Req, options: Options) => Res | Promise<Res>;

vi.mock('../api/websiteHost/sdk.gen', async (importActual) => {
  const actualImpl = await importActual<typeof import('../api/websiteHost/sdk.gen')>();
  return {
    ...actualImpl,
    refreshToken: vi.fn(() =>
      Promise.resolve({
        data: {} as EmptyResponse,
        request: {} as Request,
        response: { ok: true, status: 200 } as Response
      })
    ),
    logout: vi.fn(() =>
      Promise.resolve({
        data: {} as EmptyResponse,
        request: {} as Request,
        response: { ok: true, status: 200 } as Response
      })
    )
  };
});

describe('Handle 403 Forbidden', () => {
  let handler: ResInterceptor<any, any, any>;

  beforeEach(() => {
    initializeApiClient();
    handler = apiHost1.interceptors.response.fns[0];
  });

  it('when request fails with no response, then resolve', async () =>
    await expect(handler(undefined as Response, {} as any, {} as any)).resolves.toMatchObject(undefined));

  it('when request fails with ok response, then resolve', async () => {
    const response = { ok: true, status: 200 } as Response;

    await expect(handler(response, {} as any, {} as any)).resolves.toMatchObject(response);
  });

  it('when request fails with 403 that is not CSRF, then resolve', async () => {
    const error = {
      title: 'atitle'
    } as ProblemDetails;
    const response = {
      ok: false,
      status: 403,
      text: () => JSON.stringify(error),
      clone: () => response
    } as unknown as Response;

    await expect(handler(response, {} as any, {} as any)).resolves.toMatchObject(response);
    expect(window.location.assign).not.toHaveBeenCalled();
  });

  it('when a forbidden request that is CSRF, redirect to home', async () => {
    const error = {
      title: 'csrf_violation'
    } as ProblemDetails;
    const response = {
      ok: false,
      status: 403,
      text: () => JSON.stringify(error),
      clone: () => response
    } as unknown as Response;

    await expect(handler(response, {} as any, {} as any)).resolves.toMatchObject(response);
    expect(window.location.assign).toHaveBeenCalledWith(RoutePaths.Home);
  });
});

describe('Handle 401 Unauthorized', () => {
  let handler: ResInterceptor<any, any, any>;

  beforeEach(() => {
    initializeApiClient();
    handler = apiHost1.interceptors.response.fns[0];
  });

  it('when request fails with no response, then resolve', async () =>
    await expect(handler(undefined as Response, {} as any, {} as any)).resolves.toMatchObject(undefined));

  it('when request fails with ok response, then resolve', async () => {
    const response = { ok: true, status: 200 } as Response;

    await expect(handler(response, {} as any, {} as any)).resolves.toMatchObject(response);
  });

  it('when request fails with other error response, then resolve', async () => {
    const response = { ok: false, status: 429, text: () => '{}', clone: () => response } as unknown as Response;

    await expect(handler(response, {} as any, {} as any)).resolves.toMatchObject(response);
  });

  it('when non-retryable URL, then resolve', async () => {
    const response = { ok: false, status: 401, text: () => '{}', clone: () => response } as unknown as Response;
    const request = { url: 'https://localhost/api/auth/refresh' } as Request;

    return await expect(handler(response, request, {} as any)).resolves.toMatchObject(response);
  });

  it('when refreshing token fails with 423 error, then logout and redirect to home', async () => {
    vi.mocked(refreshToken).mockImplementationOnce((config) =>
      Promise.resolve({
        data: {} as EmptyResponse,
        request: {} as Request,
        response: { ok: false, status: 423 } as Response
      })
    );
    const response = { ok: false, status: 401, text: () => '{}', clone: () => response } as unknown as Response;

    await expect(handler(response, { url: 'https://localhost/aurl' } as Request, {} as any)).resolves.toMatchObject(
      response
    );
    expect(refreshToken).toHaveBeenCalled();
    expect(logout).toHaveBeenCalled();
    expect(window.location.assign).toHaveBeenCalledWith(RoutePaths.Home);
  });

  it('when refreshing token fails with 401 error, then logout and redirect to home', async () => {
    vi.mocked(refreshToken).mockImplementationOnce((config) =>
      Promise.resolve({
        data: {} as EmptyResponse,
        request: {} as Request,
        response: { ok: false, status: 401 } as Response
      })
    );
    const response = { ok: false, status: 401, text: () => '{}', clone: () => response } as unknown as Response;

    await expect(handler(response, { url: 'https://localhost/aurl' } as Request, {} as any)).resolves.toMatchObject(
      response
    );
    expect(refreshToken).toHaveBeenCalled();
    expect(logout).toHaveBeenCalled();
    expect(window.location.assign).toHaveBeenCalledWith(RoutePaths.Home);
  });

  it('when refreshing token fails with another error, then resolve with refresh error', async () => {
    const refreshResponse = { ok: false, status: 400 } as Response;
    vi.mocked(refreshToken).mockImplementationOnce((config) =>
      Promise.resolve({
        data: {} as EmptyResponse,
        request: {} as Request,
        response: refreshResponse
      })
    );
    const response = { ok: false, status: 401, text: () => '{}', clone: () => response } as unknown as Response;

    await expect(
      handler(response as Response, { url: 'https://localhost/aurl' } as Request, {} as any)
    ).resolves.toMatchObject(refreshResponse);
    expect(refreshToken).toHaveBeenCalled();
    expect(window.location.assign).not.toHaveBeenCalled();
  });

  it('when refreshes token and retries original request, then resolve retry response', async () => {
    const retryResponse = { ok: true, status: 200 } as Response;
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(retryResponse);
    const response = { ok: false, status: 401, text: () => '{}', clone: () => response } as unknown as Response;

    await expect(handler(response, { url: 'https://localhost/aurl' } as Request, {} as any)).resolves.toMatchObject(
      retryResponse
    );
    expect(refreshToken).toHaveBeenCalled();
    expect(fetch).toHaveBeenCalledWith('https://localhost/aurl', expect.anything());
    expect(window.location.assign).not.toHaveBeenCalled();
  });

  it('when retried request fails with unauthorized, then logout and redirect to home', async () => {
    const retryResponse = { ok: false, status: 401 } as Response;
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(retryResponse);
    const response = { ok: false, status: 401, text: () => '{}', clone: () => response } as unknown as Response;

    await expect(handler(response, { url: 'https://localhost/aurl' } as Request, {} as any)).resolves.toMatchObject(
      response
    );
    expect(fetch).toHaveBeenCalledWith('https://localhost/aurl', expect.anything());
    expect(logout).toHaveBeenCalled();
    expect(window.location.assign).toHaveBeenCalledWith(RoutePaths.Home);
  });

  it('when retried request fails with another error, then resolve retry response', async () => {
    const anotherError = { title: 'another' } as ProblemDetails;
    const retryResponse = { ok: false, status: 400, text: () => JSON.stringify(anotherError) } as unknown as Response;
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(retryResponse);
    const response = { ok: false, status: 401, text: () => '{}', clone: () => response } as unknown as Response;

    await expect(handler(response, { url: 'https://localhost/aurl' } as Request, {} as any)).resolves.toMatchObject(
      retryResponse
    );
    expect(fetch).toHaveBeenCalledWith('https://localhost/aurl', expect.anything());
    expect(window.location.assign).not.toHaveBeenCalled();
  });
});
