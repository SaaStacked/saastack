import { QueryClient } from '@tanstack/react-query';
import { act, renderHook, waitFor } from '@testing-library/react';
import React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { EmptyResponse } from '../api/apiHost1';
import { IOfflineService } from '../services/IOfflineService.ts';
import { TestingProviders } from '../testing/TestingProviders.tsx';
import { CacheKeys, useActionCommand } from './ActionCommand';
import { ApiResponse } from './Actions.ts';


interface UntenantedRequestData {
  name?: string;
  value?: string;
}

interface TenantedRequestData {
  name?: string;
  value?: string;
  organizationId?: string;
}

interface TestResponse {
  message: string;
}

class MockOfflineService implements IOfflineService {
  status: 'online' | 'offline' = 'online';

  onStatusChanged(_: (status: 'online' | 'offline') => void): () => void {
    return () => {};
  }
}

vi.mock('../providers/CurrentUserContext.tsx', () => ({
  useCurrentUser: vi.fn(() => ({
    profile: {
      id: 'auserid',
      defaultOrganizationId: 'adefaultorganizationid',
      displayName: 'adisplayname',
      isAuthenticated: true
    },
    isExecuting: false,
    isAuthenticated: true
  })),
  CurrentUserProvider: ({ children }: { children: React.ReactNode }) => <>{children}</>
}));

function createWrapper({
  children,
  offlineService,
  queryClient
}: {
  children: React.ReactNode;
  offlineService: IOfflineService;
  queryClient: QueryClient;
}) {
  return <TestingProviders queryClient={queryClient} offlineService={offlineService} children={children} />;
}

describe('useActionCommand', () => {
  const offlineService = new MockOfflineService();

  describe('given a successful untenanted request', () => {
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false
        }
      }
    });
    const mockSuccessfulRequest = vi.fn(
      async (_requestData: UntenantedRequestData): Promise<ApiResponse<TestResponse>> => {
        // Add a small delay to test loading states
        await new Promise((resolve) => setTimeout(resolve, 50));
        return {
          data: { message: 'amessage' },
          error: undefined,
          request: {} as Request,
          response: { ok: true, status: 200 } as Response
        };
      }
    );

    const unTenantedAction = () =>
      useActionCommand({
        request: mockSuccessfulRequest,
        isTenanted: false,
        onSuccess: () => vi.fn(),
        passThroughErrors: {
          400: 'BadRequest'
        },
        invalidateCacheKeys: [['acachekey']] as CacheKeys
      });

    beforeEach(() => {
      offlineService.status = 'online';

      vi.spyOn(queryClient, 'removeQueries').mockImplementation(() => {});
    });

    it('when online, should execute and return response', async () => {
      const { result } = renderHook(() => unTenantedAction(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService, queryClient })
      });

      await act(async () => result.current.execute({ aname: 'avalue' } as UntenantedRequestData));

      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      expect(result.current.isExecuting).toBe(false);
      expect(result.current.isReady).toBe(true);
      expect(result.current.lastExpectedError).toBeUndefined();
      expect(result.current.lastUnexpectedError).toBeUndefined();
      expect(result.current.lastSuccessResponse).toStrictEqual({ message: 'amessage' });
      expect(result.current.lastRequestValues).toStrictEqual({ aname: 'avalue' });
      expect(mockSuccessfulRequest).toHaveBeenCalledWith({ aname: 'avalue' });
    });

    it('when offline, should not execute the request', async () => {
      offlineService.status = 'offline';

      const { result } = renderHook(() => unTenantedAction(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService, queryClient })
      });

      await act(async () => result.current.execute({ aname: 'avalue' } as UntenantedRequestData));

      await waitFor(() => expect(result.current.isSuccess).toBe(false));

      expect(result.current.isExecuting).toBe(false);
      expect(result.current.isReady).toBe(false);
      expect(result.current.lastExpectedError).toBeUndefined();
      expect(result.current.lastUnexpectedError).toBeDefined();
      expect(result.current.lastUnexpectedError?.data).toStrictEqual(new Error('actions.errors.offline'));
      expect(result.current.lastUnexpectedError?.response.status).toBe(0);
      expect(result.current.lastUnexpectedError?.response.statusText).toBe('Internal Client Error');
      expect(result.current.lastSuccessResponse).toBeUndefined();
      expect(result.current.lastRequestValues).toBeUndefined();
      expect(mockSuccessfulRequest).not.toHaveBeenCalled();
    });

    it('should invalidate the cache key', async () => {
      const { result } = renderHook(() => unTenantedAction(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService, queryClient })
      });

      await act(async () => result.current.execute({ aname: 'avalue' } as UntenantedRequestData));

      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      expect(queryClient.removeQueries).toHaveBeenCalledWith({ queryKey: ['acachekey'], exact: false });
    });
  });

  describe('given a successful tenanted request', () => {
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false
        }
      }
    });
    const mockSuccessfulRequest = vi.fn(
      async (_requestData: UntenantedRequestData): Promise<ApiResponse<TestResponse>> => {
        // Add a small delay to test loading states
        await new Promise((resolve) => setTimeout(resolve, 50));
        return {
          data: { message: 'amessage' },
          error: undefined,
          request: {} as Request,
          response: { ok: true, status: 200 } as Response
        };
      }
    );

    const tenantedAction = () =>
      useActionCommand({
        request: mockSuccessfulRequest,
        isTenanted: true,
        onSuccess: () => vi.fn(),
        passThroughErrors: {
          400: 'BadRequest'
        },
        invalidateCacheKeys: []
      });

    beforeEach(() => {
      offlineService.status = 'online';
    });

    it('should use the organizationId from the current user', async () => {
      const { result } = renderHook(() => tenantedAction(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService, queryClient })
      });

      await act(async () => result.current.execute({ aname: 'avalue' } as TenantedRequestData));

      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      expect(result.current.isExecuting).toBe(false);
      expect(result.current.isReady).toBe(true);
      expect(result.current.lastExpectedError).toBeUndefined();
      expect(result.current.lastUnexpectedError).toBeUndefined();
      expect(result.current.lastSuccessResponse).toStrictEqual({ message: 'amessage' });
      expect(result.current.lastRequestValues).toStrictEqual({
        aname: 'avalue',
        organizationId: 'adefaultorganizationid'
      });
      expect(mockSuccessfulRequest).toHaveBeenCalledWith({ aname: 'avalue', organizationId: 'adefaultorganizationid' });
    });

    it('should use the organizationId from the request', async () => {
      const { result } = renderHook(() => tenantedAction(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService, queryClient })
      });

      await act(async () =>
        result.current.execute({ aname: 'avalue', organizationId: 'anotherorganizationid' } as TenantedRequestData)
      );

      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      expect(result.current.isExecuting).toBe(false);
      expect(result.current.isReady).toBe(true);
      expect(result.current.lastExpectedError).toBeUndefined();
      expect(result.current.lastUnexpectedError).toBeUndefined();
      expect(result.current.lastSuccessResponse).toStrictEqual({ message: 'amessage' });
      expect(result.current.lastRequestValues).toStrictEqual({
        aname: 'avalue',
        organizationId: 'anotherorganizationid'
      });
      expect(mockSuccessfulRequest).toHaveBeenCalledWith({ aname: 'avalue', organizationId: 'anotherorganizationid' });
    });
  });

  describe('given an expected error', () => {
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false
        }
      }
    });

    beforeEach(() => {
      offlineService.status = 'online';
    });

    it('when rejects error, then returns expected error', async () => {
      const error = {
        title: 'atitle',
        details: 'adetails',
        errors: [{ rule: 'arule', reason: 'areason', value: 'avalue' }]
      };
      const mockFailedRequest = vi.fn(
        async (_requestData: UntenantedRequestData): Promise<ApiResponse<EmptyResponse>> =>
          Promise.reject({
            data: undefined,
            error,
            request: {} as Request,
            response: { ok: false, status: 400 } as Response
          })
      );

      const action = () =>
        useActionCommand({
          request: mockFailedRequest,
          onSuccess: () => vi.fn(),
          passThroughErrors: {
            400: 'BadRequest'
          },
          invalidateCacheKeys: []
        });

      const { result } = renderHook(() => action(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService, queryClient })
      });

      await act(async () => result.current.execute({ aname: 'avalue' } as UntenantedRequestData));

      await waitFor(() => expect(result.current.isSuccess).toBe(false));

      expect(result.current.isExecuting).toBe(false);
      expect(result.current.isReady).toBe(true);
      expect(result.current.lastExpectedError?.code).toBe('BadRequest');
      expect(result.current.lastExpectedError?.response).toStrictEqual(error);
      expect(result.current.lastUnexpectedError).toBeUndefined();
      expect(result.current.lastSuccessResponse).toBeUndefined();
      expect(result.current.lastRequestValues).toStrictEqual({ aname: 'avalue' });
      expect(mockFailedRequest).toHaveBeenCalledWith({ aname: 'avalue' });
    });

    it('when returns error, then returns expected error', async () => {
      const error = {
        title: 'atitle',
        details: 'adetails',
        errors: [{ rule: 'arule', reason: 'areason', value: 'avalue' }]
      };
      const mockFailedRequest = vi.fn(
        async (_requestData: UntenantedRequestData): Promise<ApiResponse<EmptyResponse>> =>
          Promise.resolve({
            data: undefined,
            error,
            request: {} as Request,
            response: { ok: false, status: 400 } as Response
          })
      );

      const action = () =>
        useActionCommand({
          request: mockFailedRequest,
          onSuccess: () => vi.fn(),
          passThroughErrors: {
            400: 'BadRequest'
          },
          invalidateCacheKeys: []
        });
      const { result } = renderHook(() => action(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService, queryClient })
      });

      await act(async () => result.current.execute({ aname: 'avalue' } as UntenantedRequestData));

      await waitFor(() => expect(result.current.isSuccess).toBe(false));

      expect(result.current.isExecuting).toBe(false);
      expect(result.current.isReady).toBe(true);
      expect(result.current.lastExpectedError?.code).toBe('BadRequest');
      expect(result.current.lastExpectedError?.response).toStrictEqual(error);
      expect(result.current.lastUnexpectedError).toBeUndefined();
      expect(result.current.lastSuccessResponse).toBeUndefined();
      expect(result.current.lastRequestValues).toStrictEqual({ aname: 'avalue' });
      expect(mockFailedRequest).toHaveBeenCalledWith({ aname: 'avalue' });
    });
  });

  describe('given successful request', () => {
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false
        }
      }
    });

    beforeEach(() => {
      offlineService.status = 'online';
    });

    const mockSuccessfulRequest = vi.fn(
      async (_requestData: UntenantedRequestData): Promise<ApiResponse<TestResponse>> => {
        // Add a small delay to test loading states
        await new Promise(resolve => setTimeout(resolve, 50));
        return {
          data: { message: 'amessage' },
          error: undefined,
          request: {} as Request,
          response: { ok: true, status: 200 } as Response
        };
      }
    );

    const tenantedAction = () =>
      useActionCommand({
        request: mockSuccessfulRequest,
        isTenanted: true,
        onSuccess: () => vi.fn(),
        passThroughErrors: {
          400: 'BadRequest'
        },
        invalidateCacheKeys: []
      });

    beforeEach(() => {
      offlineService.status = 'online';
    });

    it('should call onSuccess callback', async () => {
      const { result } = renderHook(() => tenantedAction(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService, queryClient })
      });

      await act(async () =>
        result.current.execute(
          {
            aname: 'avalue',
            organizationId: 'anotherorganizationid'
          } as TenantedRequestData,
          {
            onSuccess: rez => expect(rez.response.message).toBe('amessage')
          }
        )
      );

      await waitFor(() => expect(result.current.isSuccess).toBe(true));
    });
  });

  describe('given no expected errors', () => {
    const queryClient = new QueryClient({
      defaultOptions: {
        queries: {
          retry: false
        }
      }
    });

    beforeEach(() => {
      offlineService.status = 'online';
    });

    it('when rejects JavaScript error, then return unexpected error', async () => {
      const error = new Error('anerror');
      const mockFailedRequest = vi.fn(
        async (_requestData: UntenantedRequestData): Promise<ApiResponse<EmptyResponse>> => Promise.reject(error)
      );

      const action = () =>
        useActionCommand({
          request: mockFailedRequest,
          onSuccess: () => vi.fn(),
          invalidateCacheKeys: []
        });

      const { result } = renderHook(() => action(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService, queryClient })
      });

      await act(async () => result.current.execute({ aname: 'avalue' } as UntenantedRequestData));

      await waitFor(() => expect(result.current.isSuccess).toBe(false));

      expect(result.current.isExecuting).toBe(false);
      expect(result.current.isReady).toBe(true);
      expect(result.current.lastExpectedError).toBeUndefined();
      expect(result.current.lastUnexpectedError).toBeDefined();
      expect(result.current.lastUnexpectedError?.data).toStrictEqual(error);
      expect(result.current.lastUnexpectedError?.response.status).toBe(0);
      expect(result.current.lastUnexpectedError?.response.statusText).toBe('Internal Client Error');
      expect(result.current.lastSuccessResponse).toBeUndefined();
      expect(result.current.lastRequestValues).toStrictEqual({ aname: 'avalue' });
      expect(mockFailedRequest).toHaveBeenCalledWith({ aname: 'avalue' });
    });

    it('when rejects error, then return unexpected error', async () => {
      const error = {
        title: 'atitle',
        details: 'adetails',
        errors: [{ rule: 'arule', reason: 'areason', value: 'avalue' }]
      };
      const mockFailedRequest = vi.fn(
        async (_requestData: UntenantedRequestData): Promise<ApiResponse<EmptyResponse>> =>
          Promise.reject({
            data: undefined,
            error,
            request: {} as Request,
            response: { ok: false, status: 500 } as Response
          })
      );

      const action = () =>
        useActionCommand({
          request: mockFailedRequest,
          onSuccess: () => vi.fn(),
          invalidateCacheKeys: []
        });

      const { result } = renderHook(() => action(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService, queryClient })
      });

      await act(async () => result.current.execute({ aname: 'avalue' } as UntenantedRequestData));

      await waitFor(() => expect(result.current.isSuccess).toBe(false));

      expect(result.current.isExecuting).toBe(false);
      expect(result.current.isReady).toBe(true);
      expect(result.current.lastExpectedError).toBeUndefined();
      expect(result.current.lastUnexpectedError).toBeDefined();
      expect(result.current.lastUnexpectedError?.data).toStrictEqual(error);
      expect(result.current.lastSuccessResponse).toBeUndefined();
      expect(result.current.lastRequestValues).toStrictEqual({ aname: 'avalue' });
      expect(mockFailedRequest).toHaveBeenCalledWith({ aname: 'avalue' });
    });

    it('when returns error, then return unexpected error', async () => {
      const error = {
        title: 'atitle',
        details: 'adetails',
        errors: [{ rule: 'arule', reason: 'areason', value: 'avalue' }]
      };
      const mockFailedRequest = vi.fn(
        async (_requestData: UntenantedRequestData): Promise<ApiResponse<EmptyResponse>> => ({
          data: undefined,
          error,
          request: {} as Request,
          response: { ok: false, status: 500 } as Response
        })
      );

      const action = () =>
        useActionCommand({
          request: mockFailedRequest,
          onSuccess: () => vi.fn(),
          invalidateCacheKeys: []
        });

      const { result } = renderHook(() => action(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService, queryClient })
      });

      await act(async () => result.current.execute({ aname: 'avalue' } as UntenantedRequestData));

      await waitFor(() => expect(result.current.isSuccess).toBe(false));

      expect(result.current.isExecuting).toBe(false);
      expect(result.current.isReady).toBe(true);
      expect(result.current.lastExpectedError).toBeUndefined();
      expect(result.current.lastUnexpectedError).toBeDefined();
      expect(result.current.lastUnexpectedError?.data).toStrictEqual(error);
      expect(result.current.lastSuccessResponse).toBeUndefined();
      expect(result.current.lastRequestValues).toStrictEqual({ aname: 'avalue' });
      expect(mockFailedRequest).toHaveBeenCalledWith({ aname: 'avalue' });
    });
  });
});

describe('caching', () => {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false,
        gcTime: 0, // immediate collect
        staleTime: 10000 // 10 seconds
      }
    }
  });

  describe('QueryClient', () => {
    it('when fetch with cachekey, then caches response', async () => {
      const cacheKey = ['aresource', 'res_1234567890'] as unknown[];
      const httpResponse = {
        message: 'amessage'
      } as TestResponse;

      const result = await queryClient
        .fetchQuery({
          queryKey: cacheKey,
          queryFn: async () => httpResponse
        })
        .then((res: TestResponse) => res);

      expect(result).toEqual(httpResponse);
      expect(queryClient.getQueryData(cacheKey)).toEqual(httpResponse);
    });

    it('when fetch with cachekey, then caches response', async () => {
      const cacheKey = ['aresource', 'res_1234567890'] as unknown[];
      const httpResponse = {
        message: 'amessage'
      } as TestResponse;

      const result = await queryClient
        .fetchQuery({
          queryKey: cacheKey,
          queryFn: async () => httpResponse
        })
        .then((res: TestResponse) => res);

      expect(result).toEqual(httpResponse);
      expect(queryClient.getQueryData(cacheKey)).toEqual(httpResponse);
    });

    it('when remove exact with cachekey, for single matching entry, then clears matching from cache', async () => {
      const cacheKey = ['aresource', 'res_1234567890'] as unknown[];
      const httpResponse = {
        message: 'amessage'
      } as TestResponse;
      await queryClient
        .fetchQuery({
          queryKey: cacheKey,
          queryFn: async () => httpResponse
        })
        .then((res: TestResponse) => res);

      queryClient.removeQueries({ queryKey: cacheKey, exact: true });

      expect(queryClient.getQueryData(cacheKey)).toBeUndefined();
    });

    it('when remove exact with cachekey, for multiple matching entries, then clears matching only from cache', async () => {
      const httpResponse1 = {
        message: 'amessage1'
      } as TestResponse;
      const httpResponse2 = {
        message: 'amessage2'
      } as TestResponse;
      const cacheKey1 = ['aresource', 'res_1234567890'] as unknown[];
      const cacheKey2 = ['aresource', 'res_9876543210'] as unknown[];
      await queryClient
        .fetchQuery({
          queryKey: cacheKey1,
          queryFn: async () => httpResponse1
        })
        .then((res: TestResponse) => res);
      await queryClient
        .fetchQuery({
          queryKey: cacheKey2,
          queryFn: async () => httpResponse2
        })
        .then((res: TestResponse) => res);

      queryClient.removeQueries({ queryKey: cacheKey1, exact: true });

      expect(queryClient.getQueryData(cacheKey1)).toBeUndefined();
      expect(queryClient.getQueryData(cacheKey2)).toEqual(httpResponse2);
    });

    it('when remove exact with partial cachekey, for multiple matching entries, then clears none from cache', async () => {
      const httpResponse1 = {
        message: 'amessage1'
      } as TestResponse;
      const httpResponse2 = {
        message: 'amessage2'
      } as TestResponse;
      const cacheKey1 = ['aresource', 'res_1234567890'] as unknown[];
      const cacheKey2 = ['aresource', 'res_9876543210'] as unknown[];
      await queryClient
        .fetchQuery({
          queryKey: cacheKey1,
          queryFn: async () => httpResponse1
        })
        .then((res: TestResponse) => res);
      await queryClient
        .fetchQuery({
          queryKey: cacheKey2,
          queryFn: async () => httpResponse2
        })
        .then((res: TestResponse) => res);

      queryClient.removeQueries({ queryKey: ['aresource'], exact: true });

      expect(queryClient.getQueryData(cacheKey1)).toEqual(httpResponse1);
      expect(queryClient.getQueryData(cacheKey2)).toEqual(httpResponse2);
    });

    it('when remove not exact with partial cachekey, for multiple matching entries, then clears all from cache', async () => {
      const httpResponse1 = {
        message: 'amessage1'
      } as TestResponse;
      const httpResponse2 = {
        message: 'amessage2'
      } as TestResponse;
      const cacheKey1 = ['aresource', 'res_1234567890'] as unknown[];
      const cacheKey2 = ['aresource', 'res_9876543210'] as unknown[];
      await queryClient
        .fetchQuery({
          queryKey: cacheKey1,
          queryFn: async () => httpResponse1
        })
        .then((res: TestResponse) => res);
      await queryClient
        .fetchQuery({
          queryKey: cacheKey2,
          queryFn: async () => httpResponse2
        })
        .then((res: TestResponse) => res);

      queryClient.removeQueries({ queryKey: ['aresource'], exact: false });

      expect(queryClient.getQueryData(cacheKey1)).toBeUndefined();
      expect(queryClient.getQueryData(cacheKey2)).toBeUndefined();
    });
  });
});
