import { beforeEach, describe, expect, it, vi } from 'vitest';
import axios, { AxiosError } from 'axios';
import { client as apiHost1 } from './apiHost1/client.gen';
import { homePath, initializeApiClient } from './index';
import { logout, refreshToken } from './websiteHost';

type ErrInterceptor<Err, Res, Req, Options> = (
  error: Err,
  response: Res,
  request: Req,
  options: Options
) => Err | Promise<Err>;

vi.mock('../api/websiteHost/sdk.gen', async (importActual) => {
  const actualImpl = await importActual<typeof import('../api/websiteHost/sdk.gen')>();
  return {
    ...actualImpl,
    refreshToken: vi.fn(() => Promise.resolve({ data: {}, status: 200 })),
    logout: vi.fn(() => Promise.resolve({ data: {}, status: 200 }))
  };
});

describe('Handle 403 Forbidden', () => {
  let handler: ErrInterceptor<any, any, any, any>;

  beforeEach(() => {
    initializeApiClient();
    handler = apiHost1.interceptors.error.fns[0];
  });

  it('when request fails with ordinary error, then reject', async () => {
    const error = new Error('anerror');

    await expect(handler(error, {} as any, {} as any, {} as any)).rejects.toMatchObject(error);
  });

  it('when request fails with 403 that is not CSRF, then reject', async () => {
    const error = {
      isAxiosError: true,
      config: { url: 'aurl' },
      response: {
        data: {
          title: 'atitle'
        }
      },
      status: 403
    } as AxiosError;

    await expect(handler(error, {} as any, {} as any, {} as any)).rejects.toMatchObject(error);
    expect(window.location.assign).not.toHaveBeenCalled();
  });

  it('when a forbidden request that is CSRF, redirect to home', async () => {
    const error = {
      isAxiosError: true,
      config: { url: 'aurl' },
      response: {
        data: {
          title: 'csrf_violation'
        }
      },
      status: 403
    } as AxiosError;

    await expect(handler(error, {} as any, {} as any, {} as any)).rejects.toMatchObject(error);
    expect(window.location.assign).toHaveBeenCalledWith(homePath);
  });
});

describe('Handle 401 Unauthorized', () => {
  let handler: ErrInterceptor<any, any, any, any>;

  beforeEach(() => {
    initializeApiClient();
    handler = apiHost1.interceptors.error.fns[0];
  });

  it('when request fails with ordinary error, then reject', async () => {
    const error = new Error('anerror');

    await expect(handler(error, {} as any, {} as any, {} as any)).rejects.toMatchObject(error);
  });

  it('when ignored request URL, then reject', async () => {
    const error = {
      isAxiosError: true,
      config: { url: '/api/auth/refresh' },
      response: {
        data: {
          title: 'atitle'
        }
      },
      status: 401
    } as AxiosError;

    return await expect(handler(error, {} as any, {} as any, {} as any)).rejects.toMatchObject(error);
  });

  it('when unauthorized request,  call refreshToken() and retry original API', async () => {
    vi.spyOn(axios, 'request').mockResolvedValue({ response: {}, status: 200, isAxiosError: false });
    const error = {
      isAxiosError: true,
      config: { url: 'aurl' },
      response: {},
      status: 401
    } as AxiosError;

    await expect(handler(error, {} as any, {} as any, {} as any)).resolves.toMatchObject({
      status: 200
    });
    expect(refreshToken).toHaveBeenCalled();
    expect(axios.request).toHaveBeenCalledWith({ url: 'aurl' });
    expect(window.location.assign).not.toHaveBeenCalled();
  });

  it('when refreshToken() fails with 423 error, then logout and redirect to home', async () => {
    vi.mocked(refreshToken).mockImplementationOnce((config) =>
      Promise.resolve({ config, status: 423, isAxiosError: true } as any)
    );
    const error = {
      isAxiosError: true,
      config: { url: 'aurl' },
      response: {},
      status: 401
    } as AxiosError;

    await expect(handler(error, {} as any, {} as any, {} as any)).rejects.toMatchObject({
      config: undefined,
      status: 423,
      isAxiosError: true
    });
    expect(refreshToken).toHaveBeenCalled();
    expect(logout).toHaveBeenCalled();
    expect(window.location.assign).toHaveBeenCalledWith(homePath);
  });

  it('when refreshToken() fails with other error, reject with other error', async () => {
    vi.mocked(refreshToken).mockImplementationOnce((config) =>
      Promise.resolve({ config, status: 400, isAxiosError: true } as any)
    );
    const error = {
      isAxiosError: true,
      config: { url: 'aurl' },
      response: {},
      status: 401
    } as AxiosError;

    await expect(handler(error, {} as any, {} as any, {} as any)).rejects.toMatchObject({
      config: undefined,
      status: 400,
      isAxiosError: true
    });
    expect(refreshToken).toHaveBeenCalled();
    expect(window.location.assign).not.toHaveBeenCalled();
  });

  it('when retried original API fails with unauthorized, logout and redirect to home', async () => {
    vi.spyOn(axios, 'request').mockResolvedValue({ response: {}, status: 401, isAxiosError: true });
    const error = {
      isAxiosError: true,
      config: { url: 'aurl' },
      response: {},
      status: 401
    } as AxiosError;

    await expect(handler(error, {} as any, {} as any, {} as any)).rejects.toMatchObject({
      response: {}
    });
    expect(axios.request).toHaveBeenCalledWith({ url: 'aurl' });
    expect(window.location.assign).toHaveBeenCalledWith(homePath);
  });

  it('when retried original API fails with other error, return error', async () => {
    vi.spyOn(axios, 'request').mockResolvedValue({ response: {}, status: 400, isAxiosError: true });

    const error = {
      isAxiosError: true,
      config: { url: 'aurl' },
      response: {},
      status: 401
    } as AxiosError;

    await expect(handler(error, {} as any, {} as any, {} as any)).rejects.toMatchObject({
      response: {}
    });
    expect(axios.request).toHaveBeenCalledWith({ url: 'aurl' });
    expect(window.location.assign).not.toHaveBeenCalled();
  });
});
