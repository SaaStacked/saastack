import axios from 'axios';
import { client as apiHost1 } from './apiHost1';
import { initializeApiClient } from './index';
import { client as websiteHost } from './websiteHost';
import { beforeEach, describe, expect, it, vi } from 'vitest';

interface FulfilledHandler<T> {
  (value: T): T | Promise<T>;
}

interface RejectedHandler {
  (error: any): any;
}

interface AxiosInterceptorHandleItem<V> {
  fulfilled: FulfilledHandler<V>;
  rejected: RejectedHandler;
  synchronous: boolean;
}

describe('Handle 403 Forbidden', () => {
  let handler: AxiosInterceptorHandleItem<any>;

  beforeEach(() => {
    initializeApiClient();
    // @ts-ignore
    handler = apiHost1.instance.interceptors.response.handlers[0];
  });

  it('should ignore ordinary request that succeeds', async () =>
    expect(handler.fulfilled({ data: 'adata' })).toStrictEqual({
      data: 'adata'
    }));

  it('should reject an ordinary request that fails', async () =>
    await expect(
      handler.rejected({
        config: { url: 'aurl' },
        response: {},
        status: 404
      })
    ).rejects.toMatchObject({
      response: {}
    }));

  it('should reject a forbidden request that is not CSRF', async () => {
    await expect(
      handler.rejected({
        config: { url: 'aurl' },
        response: {
          data: {
            title: 'atitle'
          }
        },
        status: 403
      })
    ).rejects.toMatchObject({
      response: {}
    });
    expect(window.location.assign).not.toHaveBeenCalled();
  });

  it('should redirect to login, when a forbidden request that is CSRF', async () => {
    await expect(
      handler.rejected({
        config: { url: 'aurl' },
        response: {
          data: {
            title: 'csrf_violation'
          }
        },
        status: 403
      })
    ).rejects.toMatchObject({
      response: {}
    });
    expect(window.location.assign).toHaveBeenCalledWith('/login');
  });
});

describe('Handle 401 Unauthorized', () => {
  let handler: AxiosInterceptorHandleItem<any>;

  beforeEach(() => {
    initializeApiClient();
    // @ts-ignore
    handler = apiHost1.instance.interceptors.response.handlers[0];
  });

  it('should ignore ordinary request that succeeds', async () =>
    expect(handler.fulfilled({ data: 'adata' })).toStrictEqual({
      data: 'adata'
    }));

  it('should reject an ordinary request that fails', async () =>
    await expect(
      handler.rejected({
        config: { url: 'aurl' },
        response: {},
        status: 404
      })
    ).rejects.toMatchObject({
      response: {}
    }));

  it('should reject an ignored request URL', async () =>
    await expect(
      handler.rejected({
        config: { url: '/api/auth/refresh' },
        response: {},
        status: 404
      })
    ).rejects.toMatchObject({
      response: {}
    }));

  it('should call refreshToken() and retry original API, when unauthorized request', async () => {
    vi.spyOn(axios, 'request').mockResolvedValue({ response: {}, status: 200, isAxiosError: false });

    await expect(
      handler.rejected({
        config: { url: 'aurl' },
        response: {},
        status: 401
      })
    ).resolves.toMatchObject({
      status: 200
    });
    expect(websiteHost.post).toHaveBeenCalledWith({ url: '/api/auth/refresh' });
    expect(axios.request).toHaveBeenCalledWith({ url: 'aurl' });
    expect(window.location.assign).not.toHaveBeenCalled();
  });

  it('should redirect to login, when refreshToken() fails with 423 error', async () => {
    vi.mocked(websiteHost.post).mockImplementationOnce((config) =>
      Promise.resolve({ config, status: 423, isAxiosError: true } as any)
    );

    await expect(
      handler.rejected({
        config: { url: 'aurl' },
        response: {},
        status: 401
      })
    ).rejects.toMatchObject({
      config: { url: '/api/auth/refresh' },
      status: 423,
      isAxiosError: true
    });
    expect(window.location.assign).toHaveBeenCalledWith('/login');
  });

  it('should reject with other error, when refreshToken() fails with other error', async () => {
    vi.mocked(websiteHost.post).mockImplementationOnce((config) =>
      Promise.resolve({ config, status: 400, isAxiosError: true } as any)
    );

    await expect(
      handler.rejected({
        config: { url: 'aurl' },
        response: {},
        status: 401
      })
    ).rejects.toMatchObject({
      config: { url: '/api/auth/refresh' },
      status: 400,
      isAxiosError: true
    });
    expect(window.location.assign).not.toHaveBeenCalled();
  });

  it('should redirect to login, when retried original API fails with unauthorized', async () => {
    vi.spyOn(axios, 'request').mockResolvedValue({ response: {}, status: 401, isAxiosError: true });

    await expect(
      handler.rejected({
        config: { url: 'aurl' },
        response: {},
        status: 401
      })
    ).rejects.toMatchObject({
      response: {}
    });
    expect(axios.request).toHaveBeenCalledWith({ url: 'aurl' });
    expect(window.location.assign).toHaveBeenCalledWith('/login');
  });

  it('should redirect to login, when retried original API fails with other error', async () => {
    vi.spyOn(axios, 'request').mockResolvedValue({ response: {}, status: 400, isAxiosError: true });

    await expect(
      handler.rejected({
        config: { url: 'aurl' },
        response: {},
        status: 401
      })
    ).rejects.toMatchObject({
      response: {}
    });
    expect(axios.request).toHaveBeenCalledWith({ url: 'aurl' });
    expect(window.location.assign).not.toHaveBeenCalled();
  });
});
