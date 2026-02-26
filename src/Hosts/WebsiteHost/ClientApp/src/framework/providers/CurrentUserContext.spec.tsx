import { QueryClient } from '@tanstack/react-query';
import { renderHook, waitFor } from '@testing-library/react';
import React from 'react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { OrganizationErrorCodes } from '../../subDomains/organizations/actions/getOrganization.ts';
import { ApiResponse } from '../actions/Actions.ts';
import {
  getOrganization,
  GetOrganizationResponse,
  getProfileForCaller,
  GetProfileForCallerResponse,
  UserProfileClassification,
  UserProfileForCaller
} from '../api/apiHost1';
import { logout, refreshToken } from '../api/websiteHost';
import { anonymousUser } from '../constants';
import { IOfflineService } from '../services/IOfflineService.ts';
import { TestingProviders } from '../testing/TestingProviders.tsx';
import { useCurrentUser } from './CurrentUserContext.tsx';

vi.mock('../api/apiHost1/sdk.gen', () => ({
  getProfileForCaller: vi.fn(),
  getOrganization: vi.fn()
}));

vi.mock('../api/websiteHost/sdk.gen', () => ({
  logout: vi.fn(),
  refreshToken: vi.fn()
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
  const mockGetOrganization = vi.mocked(getOrganization);
  const mockLogout = vi.mocked(logout);
  const mockRefreshToken = vi.mocked(refreshToken);

  beforeEach(() => {
    mockGetProfileForCaller.mockReset();
    mockGetOrganization.mockReset();
    mockLogout.mockReset();
    mockRefreshToken.mockReset();
    vi.useFakeTimers({ shouldAdvanceTime: true });
  });

  afterEach(() => vi.useRealTimers());

  it('when anonymous user, should return user', async () => {
    mockGetProfileForCaller.mockResolvedValueOnce({
      data: { profile: anonymousUser },
      error: undefined,
      request: {} as Request,
      response: { ok: true, status: 200 } as Response
    } as ApiResponse<GetProfileForCallerResponse>);

    const { result } = renderHook(() => useCurrentUser(), {
      wrapper: ({ children }) => createWrapper({ children, offlineService })
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.profile).toStrictEqual(anonymousUser);
    expect(result.current.organization).toBeUndefined();
    expect(mockGetOrganization).not.toHaveBeenCalled();
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
      defaultOrganizationId: 'adefaultorganizationid',
      features: [],
      roles: []
    } as unknown as UserProfileForCaller;

    mockGetProfileForCaller.mockResolvedValueOnce({
      data: { profile: authenticatedUser },
      error: undefined,
      request: {} as Request,
      response: { ok: true, status: 200 } as Response
    } as ApiResponse<GetProfileForCallerResponse>);
    const organization = {
      id: 'anorganizationid',
      name: 'anorganizationname',
      createdById: 'auserid',
      ownership: 'Shared'
    };
    mockGetOrganization.mockResolvedValue({
      data: { organization },
      error: undefined,
      request: {} as Request,
      response: { ok: true, status: 200 } as Response
    } as ApiResponse<GetOrganizationResponse>);

    const { result } = renderHook(() => useCurrentUser(), {
      wrapper: ({ children }) => createWrapper({ children, offlineService })
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.isAuthenticated).toBe(true);
    expect(result.current.profile).toStrictEqual(authenticatedUser);
    expect(result.current.organization).toStrictEqual(organization);
    expect(mockGetOrganization).toHaveBeenCalledWith({
      path: {
        Id: 'adefaultorganizationid'
      }
    });
  });

  it('when no profile in response, should return anonymous user', async () => {
    mockGetProfileForCaller.mockResolvedValueOnce({
      data: undefined as any,
      error: undefined,
      request: {} as Request,
      response: { ok: true, status: 200 } as Response
    } as ApiResponse<GetProfileForCallerResponse>);

    const { result } = renderHook(() => useCurrentUser(), {
      wrapper: ({ children }) => createWrapper({ children, offlineService })
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(true));

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.profile).toStrictEqual(anonymousUser);
    expect(result.current.organization).toBeUndefined();
    expect(mockGetOrganization).not.toHaveBeenCalled();
  });

  it('when XHR fails, should return anonymous user', async () => {
    mockGetProfileForCaller.mockRejectedValueOnce({
      data: undefined,
      error: {
        title: 'atitle',
        details: 'adetails'
      },
      request: {} as Request,
      response: { ok: false, status: 500 } as Response
    } as ApiResponse<GetProfileForCallerResponse>);

    const { result } = renderHook(() => useCurrentUser(), {
      wrapper: ({ children }) => createWrapper({ children, offlineService })
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(false));

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.profile).toStrictEqual(anonymousUser);
    expect(result.current.organization).toBeUndefined();
    expect(mockGetOrganization).not.toHaveBeenCalled();
  });

  it('when XHR fails, should logout', async () => {
    mockGetProfileForCaller.mockRejectedValue({
      data: undefined,
      error: {
        title: 'atitle',
        details: 'adetails'
      },
      request: {} as Request,
      response: { ok: false, status: 500 } as Response
    } as ApiResponse<GetProfileForCallerResponse>);

    const { result } = renderHook(() => useCurrentUser(), {
      wrapper: ({ children }) => createWrapper({ children, offlineService })
    });

    await waitFor(() => expect(result.current.isSuccess).toBe(false));

    expect(result.current.isAuthenticated).toBe(false);
    expect(result.current.profile).toStrictEqual(anonymousUser);
    expect(mockLogout).toHaveBeenCalled();
    expect(result.current.organization).toBeUndefined();
    expect(mockGetOrganization).not.toHaveBeenCalled();
  });

  it('when organization fetch returns 403, should delay, refresh token and retry', async () => {
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
      defaultOrganizationId: 'adefaultorganizationid',
      features: [],
      roles: []
    } as unknown as UserProfileForCaller;

    const organization = {
      id: 'anorganizationid',
      name: 'anorganizationname',
      createdById: 'auserid',
      ownership: 'Shared'
    };

    mockGetProfileForCaller.mockResolvedValueOnce({
      data: { profile: authenticatedUser },
      error: undefined,
      request: {} as Request,
      response: { ok: true, status: 200 } as Response
    } as ApiResponse<GetProfileForCallerResponse>);

    // Sequence the calls
    mockGetOrganization
      .mockResolvedValueOnce({
        data: undefined,
        error: {
          type: 'https://tools.ietf.org/html/rfc7231#section-6.5.3',
          title: 'Forbidden',
          status: 403,
          detail: 'Access forbidden',
          instance: '/organizations/adefaultorganizationid',
          code: OrganizationErrorCodes.forbidden
        },
        request: {} as Request,
        response: { ok: false, status: 403 } as Response
      } as any)
      .mockResolvedValueOnce({
        data: { organization },
        error: undefined,
        request: {} as Request,
        response: { ok: true, status: 200 } as Response
      } as ApiResponse<GetOrganizationResponse>);

    mockRefreshToken.mockResolvedValueOnce({
      data: {},
      error: undefined,
      request: {} as Request,
      response: { ok: true, status: 201 } as Response
    } as any);

    renderHook(() => useCurrentUser(), {
      wrapper: ({ children }) => createWrapper({ children, offlineService })
    });

    await waitFor(() => expect(mockGetOrganization).toHaveBeenCalledTimes(2));
    await vi.advanceTimersByTimeAsync(2000);
    await waitFor(() => expect(mockRefreshToken).toHaveBeenCalled());

    expect(mockGetOrganization).toHaveBeenCalledWith({
      path: {
        Id: 'adefaultorganizationid'
      }
    });
  });

  it('when organization fetch returns 403, and token refresh fails, should not retry', async () => {
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
      defaultOrganizationId: 'adefaultorganizationid',
      features: [],
      roles: []
    } as unknown as UserProfileForCaller;

    mockGetProfileForCaller.mockResolvedValueOnce({
      data: { profile: authenticatedUser },
      error: undefined,
      request: {} as Request,
      response: { ok: true, status: 200 } as Response
    } as ApiResponse<GetProfileForCallerResponse>);

    // Organization fetch returns 403 error
    mockGetOrganization.mockResolvedValueOnce({
      data: undefined,
      error: {
        type: 'https://tools.ietf.org/html/rfc7231#section-6.5.3',
        title: 'Forbidden',
        status: 403,
        detail: 'Access forbidden',
        instance: '/organizations/adefaultorganizationid',
        code: OrganizationErrorCodes.forbidden
      },
      request: {} as Request,
      response: { ok: false, status: 403 } as Response
    } as any);

    mockRefreshToken.mockResolvedValueOnce({
      data: undefined,
      error: {
        title: 'Unauthorized',
        details: 'Token refresh failed'
      },
      request: {} as Request,
      response: { ok: false, status: 401 } as Response
    } as any);

    renderHook(() => useCurrentUser(), {
      wrapper: ({ children }) => createWrapper({ children, offlineService })
    });

    await waitFor(() => expect(mockGetOrganization).toHaveBeenCalledTimes(3));
    await vi.advanceTimersByTimeAsync(2000);
    await waitFor(() => expect(mockRefreshToken).toHaveBeenCalled());

    expect(mockRefreshToken).toHaveBeenCalledTimes(1);
    expect(mockGetOrganization).toHaveBeenCalledTimes(3);
  });
});
