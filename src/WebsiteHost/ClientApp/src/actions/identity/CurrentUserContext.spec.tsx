import { beforeEach, describe, expect, it, vi } from 'vitest';
import { act, renderHook, waitFor } from '@testing-library/react';
import '@testing-library/jest-dom';
import {
  getProfileForCaller,
  GetProfileForCallerResponse,
  UserProfileClassification,
  UserProfileForCaller
} from '../../api/apiHost1';
import { anonymousUser } from '../../constants.ts';
import { AxiosError, AxiosResponse } from 'axios';
import { IOfflineService } from '../../services/IOfflineService.tsx';
import { OfflineServiceProvider } from '../../services/OfflineServiceContext.tsx';
import { CurrentUserProvider, useCurrentUser } from './CurrentUserContext.tsx';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { logout } from '../../api/websiteHost';

vi.mock('../../api/apiHost1/services.gen', () => ({
  getProfileForCaller: vi.fn()
}));

vi.mock('../../api/websiteHost/services.gen', () => ({
  logout: vi.fn()
}));

class MockOfflineService implements IOfflineService {
  status: 'online' | 'offline' = 'online';

  onStatusChanged(_: (status: 'online' | 'offline') => void): () => void {
    return () => {};
  }
}

function createWrapper({ children, offlineService }: { children: React.ReactNode; offlineService: IOfflineService }) {
  const queryClient = new QueryClient({
    defaultOptions: {
      queries: {
        retry: false
      }
    }
  });
  return (
    <QueryClientProvider client={queryClient}>
      <OfflineServiceProvider offlineService={offlineService}>
        <CurrentUserProvider>{children}</CurrentUserProvider>
      </OfflineServiceProvider>
    </QueryClientProvider>
  );
}

describe('CurrentUserContext', () => {
  const offlineService = new MockOfflineService();
  const mockGetProfileForCaller = vi.mocked(getProfileForCaller);
  const mockLogout = vi.mocked(logout);

  beforeEach(() => {
    mockGetProfileForCaller.mockReset();
    mockLogout.mockReset();
  });

  it('when anonymous user, should return user', async () => {
    mockGetProfileForCaller.mockResolvedValueOnce({
      data: { profile: anonymousUser },
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {} as any,
      request: {},
      error: undefined
    } as AxiosResponse<GetProfileForCallerResponse>);

    const { result } = renderHook(() => useCurrentUser(), {
      wrapper: ({ children }) => createWrapper({ children, offlineService })
    });

    await act(async () => {
      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      expect(result.current.isAuthenticated).toBe(false);
      expect(result.current.profile).toStrictEqual(anonymousUser);
    });
  });

  it('when authenticated user, should return user', async () => {
    const authenticatedUser = {
      id: 'auserid',
      userId: 'auserid',
      isAuthenticated: true,
      address: {
        countryCode: 'USA'
      },
      avatarUrl: null,
      classification: 'Person' as UserProfileClassification,
      displayName: 'adisplayname',
      emailAddress: null,
      name: {
        firstName: 'afirstname'
      },
      phoneNumber: null,
      timezone: null,
      defaultOrganizationId: null,
      features: [],
      roles: []
    } as unknown as UserProfileForCaller;

    mockGetProfileForCaller.mockResolvedValueOnce({
      data: {
        profile: authenticatedUser
      },
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {} as any,
      request: {},
      error: undefined
    } as AxiosResponse<GetProfileForCallerResponse>);

    const { result } = renderHook(() => useCurrentUser(), {
      wrapper: ({ children }) => createWrapper({ children, offlineService })
    });

    await act(async () => {
      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      expect(result.current.isAuthenticated).toBe(true);
      expect(result.current.profile).toStrictEqual(authenticatedUser);
    });
  });

  it('when no profile in response, should return anonymous user', async () => {
    mockGetProfileForCaller.mockResolvedValueOnce({
      data: undefined as any,
      status: 200,
      statusText: 'OK',
      headers: {},
      config: {} as any,
      request: {},
      error: undefined
    } as AxiosResponse<GetProfileForCallerResponse>);

    const { result } = renderHook(() => useCurrentUser(), {
      wrapper: ({ children }) => createWrapper({ children, offlineService })
    });

    await act(async () => {
      await waitFor(() => expect(result.current.isSuccess).toBe(true));

      expect(result.current.isAuthenticated).toBe(false);
      expect(result.current.profile).toStrictEqual(anonymousUser);
    });
  });

  it('when XHR fails, should return anonymous user', async () => {
    mockGetProfileForCaller.mockRejectedValueOnce({
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
    } as AxiosError as Error);

    const { result } = renderHook(() => useCurrentUser(), {
      wrapper: ({ children }) => createWrapper({ children, offlineService })
    });

    await act(async () => {
      await waitFor(() => expect(result.current.isSuccess).toBe(false));

      expect(result.current.isAuthenticated).toBe(false);
      expect(result.current.profile).toStrictEqual(anonymousUser);
    });
  });

  it('when XHR fails, should logout', async () => {
    mockGetProfileForCaller.mockRejectedValue({
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
    } as AxiosError);

    const { result } = renderHook(() => useCurrentUser(), {
      wrapper: ({ children }) => createWrapper({ children, offlineService })
    });

    await act(async () => {
      await waitFor(() => expect(result.current.isSuccess).toBe(false));

      expect(result.current.isAuthenticated).toBe(false);
      expect(result.current.profile).toStrictEqual(anonymousUser);
      expect(mockLogout).toHaveBeenCalled();
    });
  });
});
