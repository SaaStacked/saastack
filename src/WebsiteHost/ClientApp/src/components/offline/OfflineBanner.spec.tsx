import { act, render, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import '@testing-library/jest-dom';
import type { IOfflineService } from '../../services/IOfflineService';
import { OfflineServiceContext } from '../../services/OfflineServiceContext';
import { animationDurationInMs, OfflineBanner } from './OfflineBanner';


describe('OfflineBanner', () => {
  const mockOfflineService: IOfflineService = {
    status: 'online',
    onStatusChanged: vi.fn((callback) => {
      statusChangeCallback = callback;
      return vi.fn();
    })
  };

  let statusChangeCallback: (status: 'online' | 'offline') => void;

  beforeEach(() => vi.useFakeTimers());

  afterEach(() => vi.useRealTimers());

  const renderWithContext = (service: IOfflineService) =>
    render(
      <OfflineServiceContext.Provider value={{ offlineService: service }}>
        <OfflineBanner />
      </OfflineServiceContext.Provider>
    );

  it('when service is online, not render banner', () => {
    renderWithContext(mockOfflineService);

    expect(screen.queryByText(/components.offline.error/)).not.toBeInTheDocument();
  });

  it('when service starts offline, renders banner after animation timeout', async () => {
    (mockOfflineService as any).status = 'offline';

    renderWithContext(mockOfflineService);

    act(() => vi.advanceTimersByTime(10));

    expect(screen.getByText(/components.offline.error/)).toBeInTheDocument();
  });

  it('when service is online, status changes to offline and shows banner', async () => {
    (mockOfflineService as any).status = 'online';
    renderWithContext(mockOfflineService);

    expect(screen.queryByText(/components.offline.error/)).not.toBeInTheDocument();

    act(() => statusChangeCallback('offline'));

    // Wait for animation timeout
    act(() => vi.advanceTimersByTime(10));

    expect(screen.getByText(/components.offline.error/)).toBeInTheDocument();
  });

  it('when service is offline, status changes to online and hides banner', async () => {
    (mockOfflineService as any).status = 'offline';
    renderWithContext(mockOfflineService);

    // Wait for initial animation
    act(() => vi.advanceTimersByTime(10));

    expect(screen.getByText(/components.offline.error/)).toBeInTheDocument();

    act(() => statusChangeCallback('online'));

    // Wait for exit animation
    act(() => vi.advanceTimersByTime(animationDurationInMs));

    expect(screen.queryByText(/components.offline.error/)).not.toBeInTheDocument();
  });

  it('when component unmounts, unsubscribes from service', () => {
    const unsubscribeMock = vi.fn();
    mockOfflineService.onStatusChanged = vi.fn(() => unsubscribeMock);

    const { unmount } = renderWithContext(mockOfflineService);

    unmount();

    expect(unsubscribeMock).toHaveBeenCalled();
  });
});
