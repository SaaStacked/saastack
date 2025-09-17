import { QueryClient } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import React, { act } from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AxiosError, AxiosResponse } from 'axios';
import { IOfflineService } from '../services/IOfflineService';
import { TestingProviders } from '../testing/TestingProviders.tsx';
import useActionQuery from './ActionQuery';
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
      useActionQuery({
        request: mockSuccessfulRequest as any,
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
      expect(result.current.lastUnexpectedError).toBeUndefined();
      expect(result.current.lastSuccessResponse).toBeUndefined();
      expect(result.current.lastRequestValues).toBeUndefined();
      expect(mockSuccessfulRequest).not.toHaveBeenCalled();
    });
  });

  describe('given a successful tenanted request', () => {
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
      useActionQuery({
        request: mockSuccessfulRequest as any,
        isTenanted: true,
        transform: (x) => x,
        cacheKey: ['acachekey'],
        passThroughErrors: {
          400: 'BadRequest'
        }
      });

    const tenantedActionWithOrganizationId = () =>
      useActionQuery({
        request: mockSuccessfulRequest as any,
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
    const mockExpectedErrorRequest = vi.fn(() =>
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

    const anyAction = () =>
      useActionQuery({
        request: mockExpectedErrorRequest as any,
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

    it('should execute and return expected error', async () => {
      const { result } = renderHook(() => anyAction(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService })
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
      expect(mockExpectedErrorRequest).toHaveBeenCalledWith({ aname: 'avalue' });
    });
  });

  describe('given an unexpected error', () => {
    const mockUnexpectedErrorRequest = vi.fn(() =>
      Promise.reject({
        isAxiosError: true,
        message: 'anerror',
        response: {
          status: 500,
          statusText: 'anerror',
          data: {
            title: 'atitle',
            details: 'adetails'
          },
          headers: {},
          config: {} as any
        }
      } as AxiosError)
    );

    const anyAction = () =>
      useActionQuery({
        request: mockUnexpectedErrorRequest as any,
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

    it('should execute and return unexpected error', async () => {
      const { result } = renderHook(() => anyAction(), {
        wrapper: ({ children }) => createWrapper({ children, offlineService })
      });

      await act(async () => result.current.execute({ aname: 'avalue' } as UntenantedRequestData));

      await waitFor(() => expect(result.current.isSuccess).toBe(false));

      expect(result.current.isExecuting).toBe(false);
      expect(result.current.isReady).toBe(true);
      expect(result.current.lastUnexpectedError).not.toBeUndefined();
      expect(result.current.lastUnexpectedError?.message).toBe('anerror');
      expect(result.current.lastUnexpectedError?.response?.data).toStrictEqual({
        title: 'atitle',
        details: 'adetails'
      });
      expect(result.current.lastSuccessResponse).toBeUndefined();
      expect(result.current.lastRequestValues).toStrictEqual({ aname: 'avalue' });
      expect(mockUnexpectedErrorRequest).toHaveBeenCalledWith({ aname: 'avalue' });
    });
  });
});
