import { QueryClient } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import React, { act } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { EmptyResponse } from '../api/apiHost1';
import { IOfflineService } from '../services/IOfflineService.ts';
import { TestingProviders } from '../testing/TestingProviders.tsx';
import { useActionCommand } from './ActionCommand.ts';
import { useActionQuery } from './ActionQuery';
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

function createWrapper({ children, offlineService }: { children: React.ReactNode; offlineService: IOfflineService }) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });

  return <TestingProviders queryClient={queryClient} offlineService={offlineService} children={children} />;
}

describe('useActionQuery', () => {
  const offlineService = new MockOfflineService();

  describe('given a successful untenanted request', () => {
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
      useActionQuery({
        request: mockSuccessfulRequest,
        isTenanted: false,
        transform: (x) => x,
        cacheKey: ['acachekey'],
        passThroughErrors: {
          400: 'BadRequest'
        }
      });

    beforeEach(() => {
      offlineService.status = 'online';
    });

    it('when online, should execute and return response', async () => {
      const { result } = renderHook(() => unTenantedAction(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService })
      });

      await act(async () => result.current.execute({ aname: 'avalue' } as UntenantedRequestData));

      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      expect(result.current.isExecuting).toBe(false);
      expect(result.current.isReady).toBe(true);
      expect(result.current.lastUnexpectedError).toBeUndefined();
      expect(result.current.lastSuccessResponse).toStrictEqual({ message: 'amessage' });
      expect(result.current.lastRequestValues).toStrictEqual({ aname: 'avalue' });
      expect(mockSuccessfulRequest).toHaveBeenCalledWith({ aname: 'avalue' });
    });

    it('when offline, should not execute the request', async () => {
      offlineService.status = 'offline';

      const { result } = renderHook(() => unTenantedAction(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService })
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
  });

  describe('given a successful tenanted request', () => {
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
      useActionQuery({
        request: mockSuccessfulRequest,
        isTenanted: true,
        transform: (x) => x,
        cacheKey: ['acachekey'],
        passThroughErrors: {
          400: 'BadRequest'
        }
      });

    const tenantedActionWithOrganizationId = () =>
      useActionQuery({
        request: mockSuccessfulRequest,
        isTenanted: true,
        transform: (x) => x,
        cacheKey: ['acachekey'],
        passThroughErrors: {
          400: 'BadRequest'
        }
      });

    beforeEach(() => {
      offlineService.status = 'online';
    });

    it('should use the organizationId from the current user', async () => {
      const { result } = renderHook(() => tenantedAction(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService })
      });

      await act(async () => result.current.execute({ aname: 'avalue' } as TenantedRequestData));

      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      expect(result.current.isExecuting).toBe(false);
      expect(result.current.isReady).toBe(true);
      expect(result.current.lastUnexpectedError).toBeUndefined();
      expect(result.current.lastSuccessResponse).toStrictEqual({ message: 'amessage' });
      expect(result.current.lastRequestValues).toStrictEqual({
        aname: 'avalue',
        organizationId: 'adefaultorganizationid'
      });
      expect(mockSuccessfulRequest).toHaveBeenCalledWith({ aname: 'avalue', organizationId: 'adefaultorganizationid' });
    });

    it('should use the organizationId from the request', async () => {
      const { result } = renderHook(() => tenantedActionWithOrganizationId(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService })
      });

      await act(async () =>
        result.current.execute({ aname: 'avalue', organizationId: 'anotherorganizationid' } as TenantedRequestData)
      );

      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      expect(result.current.isExecuting).toBe(false);
      expect(result.current.isReady).toBe(true);
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

      const anyAction = () =>
        useActionQuery({
          request: mockFailedRequest,
          isTenanted: false,
          transform: (x) => x,
          cacheKey: ['acachekey'],
          passThroughErrors: {
            400: 'BadRequest'
          }
        });

      const { result } = renderHook(() => anyAction(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService })
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

      const anyAction = () =>
        useActionQuery({
          request: mockFailedRequest,
          isTenanted: false,
          transform: (x) => x,
          cacheKey: ['acachekey'],
          passThroughErrors: {
            400: 'BadRequest'
          }
        });

      const { result } = renderHook(() => anyAction(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService })
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

  describe('given no expected errors', () => {
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
        wrapper: ({ children }) => createWrapper({ children, offlineService })
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

      const anyAction = () =>
        useActionQuery({
          request: mockFailedRequest,
          isTenanted: false,
          transform: (x) => x,
          cacheKey: ['acachekey']
        });

      const { result } = renderHook(() => anyAction(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService })
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

      const anyAction = () =>
        useActionQuery({
          request: mockFailedRequest,
          isTenanted: false,
          transform: (x) => x,
          cacheKey: ['acachekey']
        });

      const { result } = renderHook(() => anyAction(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService })
      });

      await act(async () => result.current.execute({ aname: 'avalue' } as UntenantedRequestData));

      await waitFor(() => expect(result.current.isSuccess).toBe(false));

      expect(result.current.isExecuting).toBe(false);
      expect(result.current.isReady).toBe(true);
      expect(result.current.lastExpectedError).toBeUndefined();
      expect(result.current.lastUnexpectedError).toBeDefined();
      expect(result.current.lastUnexpectedError?.data).toStrictEqual({
        title: 'atitle',
        details: 'adetails',
        errors: [{ rule: 'arule', reason: 'areason', value: 'avalue' }]
      });
      expect(result.current.lastSuccessResponse).toBeUndefined();
      expect(result.current.lastRequestValues).toStrictEqual({ aname: 'avalue' });
      expect(mockFailedRequest).toHaveBeenCalledWith({ aname: 'avalue' });
    });
  });
});
