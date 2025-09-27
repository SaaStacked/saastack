import { QueryClient } from '@tanstack/react-query';
import { act, renderHook, waitFor } from '@testing-library/react';
import React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AxiosError, AxiosResponse } from 'axios';
import { IOfflineService } from '../services/IOfflineService.ts';
import { TestingProviders } from '../testing/TestingProviders.tsx';
import { useActionCommand } from './ActionCommand';
import { ActionRequestData } from './Actions';


interface UntenantedRequestData extends ActionRequestData {
  name?: string;
  value?: string;
}

interface TenantedRequestData extends ActionRequestData {
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
      async (_requestData: UntenantedRequestData): Promise<AxiosResponse<TestResponse>> => {
        // Add a small delay to test loading states
        await new Promise((resolve) => setTimeout(resolve, 50));
        return {
          data: { message: 'amessage' },
          status: 200,
          statusText: 'OK',
          headers: {},
          config: {} as any,
          request: {},
          error: undefined
        } as AxiosResponse<TestResponse>;
      }
    );

    const unTenantedAction = () =>
      useActionCommand({
        request: mockSuccessfulRequest as any,
        isTenanted: false,
        onSuccess: () => vi.fn(),
        passThroughErrors: {
          400: 'BadRequest'
        },
        invalidateCacheKeys: ['acachekey']
      });

    beforeEach(() => {
      offlineService.status = 'online';

      vi.spyOn(queryClient, 'invalidateQueries').mockImplementation(() => Promise.resolve());
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
      expect(result.current.lastUnexpectedError).toBeUndefined();
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

      expect(queryClient.invalidateQueries).toHaveBeenCalledWith({ queryKey: ['acachekey'], exact: false });
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
      async (_requestData: TenantedRequestData): Promise<AxiosResponse<TestResponse>> => {
        // Add a small delay to test loading states
        await new Promise((resolve) => setTimeout(resolve, 50));
        return {
          data: { message: 'amessage' },
          status: 200,
          statusText: 'OK',
          headers: {},
          config: {} as any,
          request: {},
          error: undefined
        } as AxiosResponse<TestResponse>;
      }
    );

    const tenantedAction = () =>
      useActionCommand({
        request: mockSuccessfulRequest as any,
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

    it('when throws error, return expected error', async () => {
      const mockRequest = vi.fn(() =>
        Promise.reject({
          isAxiosError: true,
          message: 'anerror',
          response: {
            status: 400,
            statusText: 'anerror',
            data: {
              title: 'atitle',
              details: 'adetails',
              errors: [{ rule: 'arule', reason: 'areason', value: 'avalue' }]
            },
            headers: {},
            config: {} as any
          }
        } as AxiosError)
      );

      const action = () =>
        useActionCommand({
          request: mockRequest as any,
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
      expect(result.current.lastExpectedError?.response).toStrictEqual({
        title: 'atitle',
        details: 'adetails',
        errors: [{ rule: 'arule', reason: 'areason', value: 'avalue' }]
      });
      expect(result.current.lastUnexpectedError).toBeUndefined();
      expect(result.current.lastSuccessResponse).toBeUndefined();
      expect(result.current.lastRequestValues).toStrictEqual({ aname: 'avalue' });
      expect(mockRequest).toHaveBeenCalledWith({ aname: 'avalue' });
    });

    it('when returns error, return expected error', async () => {
      const mockRequest = vi.fn(() =>
        Promise.resolve({
          isAxiosError: true,
          message: 'anerror',
          response: {
            status: 400,
            statusText: 'anerror',
            data: {
              title: 'atitle',
              details: 'adetails',
              errors: [{ rule: 'arule', reason: 'areason', value: 'avalue' }]
            },
            headers: {},
            config: {} as any
          }
        } as AxiosError)
      );

      const action = () =>
        useActionCommand({
          request: mockRequest as any,
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
      expect(result.current.lastExpectedError?.response).toStrictEqual({
        title: 'atitle',
        details: 'adetails',
        errors: [{ rule: 'arule', reason: 'areason', value: 'avalue' }]
      });
      expect(result.current.lastUnexpectedError).toBeUndefined();
      expect(result.current.lastSuccessResponse).toBeUndefined();
      expect(result.current.lastRequestValues).toStrictEqual({ aname: 'avalue' });
      expect(mockRequest).toHaveBeenCalledWith({ aname: 'avalue' });
    });
  });

  describe('given an unexpected error', () => {
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

    it('when throws error, return unexpected error', async () => {
      const mockRequest = vi.fn(() =>
        Promise.reject({
          isAxiosError: true,
          message: 'anerror',
          response: {
            status: 500,
            statusText: 'anerror',
            data: {
              title: 'atitle',
              details: 'adetails',
              errors: [{ rule: 'arule', reason: 'areason', value: 'avalue' }]
            },
            headers: {},
            config: {} as any
          }
        } as AxiosError)
      );

      const action = () =>
        useActionCommand({
          request: mockRequest as any,
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
      expect(result.current.lastExpectedError).toBeUndefined();
      expect(result.current.lastUnexpectedError).toBeDefined();
      expect(result.current.lastUnexpectedError?.message).toBe('anerror');
      expect(result.current.lastUnexpectedError?.response?.data).toStrictEqual({
        title: 'atitle',
        details: 'adetails',
        errors: [{ rule: 'arule', reason: 'areason', value: 'avalue' }]
      });
      expect(result.current.lastSuccessResponse).toBeUndefined();
      expect(result.current.lastRequestValues).toStrictEqual({ aname: 'avalue' });
      expect(mockRequest).toHaveBeenCalledWith({ aname: 'avalue' });
    });

    it('when returns error, return unexpected error', async () => {
      const mockRequest = vi.fn(() =>
        Promise.resolve({
          isAxiosError: true,
          message: 'anerror',
          response: {
            status: 500,
            statusText: 'anerror',
            data: {
              title: 'atitle',
              details: 'adetails',
              errors: [{ rule: 'arule', reason: 'areason', value: 'avalue' }]
            },
            headers: {},
            config: {} as any
          }
        } as AxiosError)
      );

      const action = () =>
        useActionCommand({
          request: mockRequest as any,
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
      expect(result.current.lastExpectedError).toBeUndefined();
      expect(result.current.lastUnexpectedError).toBeDefined();
      expect(result.current.lastUnexpectedError?.message).toBe('anerror');
      expect(result.current.lastUnexpectedError?.response?.data).toStrictEqual({
        title: 'atitle',
        details: 'adetails',
        errors: [{ rule: 'arule', reason: 'areason', value: 'avalue' }]
      });
      expect(result.current.lastSuccessResponse).toBeUndefined();
      expect(result.current.lastRequestValues).toStrictEqual({ aname: 'avalue' });
      expect(mockRequest).toHaveBeenCalledWith({ aname: 'avalue' });
    });
  });
});
