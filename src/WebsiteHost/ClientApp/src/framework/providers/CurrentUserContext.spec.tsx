import { QueryClient } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { AxiosError, AxiosResponse } from 'axios';
import { getProfileForCaller, GetProfileForCallerResponse, UserProfileClassification, UserProfileForCaller } from '../api/apiHost1';
import { logout } from '../api/websiteHost';
import { anonymousUser } from '../constants';
import { IOfflineService } from '../services/IOfflineService.ts';
import { TestingProviders } from '../testing/TestingProviders.tsx';
import { useCurrentUser } from './CurrentUserContext.tsx';


vi.mock('../api/apiHost1/services.gen', () => ({
  getProfileForCaller: vi.fn()
}));

vi.mock('../api/websiteHost/services.gen', () => ({
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
  return <TestingProviders queryClient={queryClient} offlineService={offlineService} children={children} />;
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

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.profile).toStrictEqual(anonymousUser);
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

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.profile).toStrictEqual(authenticatedUser);
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

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.profile).toStrictEqual(anonymousUser);
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

    await waitFor(() => expect(result.current.isSuccess).toBe(false));

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.profile).toStrictEqual(anonymousUser);
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

    await waitFor(() => expect(result.current.isSuccess).toBe(false));

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.profile).toStrictEqual(anonymousUser);
    expect(mockLogout).toHaveBeenCalled();
  });
});
