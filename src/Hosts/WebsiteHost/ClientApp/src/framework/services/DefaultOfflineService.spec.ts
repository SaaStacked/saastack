import { beforeEach, describe, expect, it, vi } from 'vitest';
import '@testing-library/jest-dom';
import { DefaultOfflineService, PeriodicCheckIntervalInMs } from './DefaultOfflineService';

global.fetch = vi.fn();
global.setInterval = vi.fn();
global.clearInterval = vi.fn();
global.AbortSignal = {
  timeout: vi.fn().mockReturnValue({ aborted: false })
} as any;

describe('DefaultOfflineService', () => {
  let service: DefaultOfflineService;
  let intervalCallback: Function;

  beforeEach(() => {
    vi.spyOn(window, 'fetch');
    vi.mocked(global.setInterval).mockImplementation((callback: Function, _delay?: number, ..._: any[]) => {
      intervalCallback = callback;
      return 123 as any;
    });
    // @ts-ignore
    global.window.navigator['onLine'] = true;
  });

  describe('constructor', () => {
    it('when navigator is online, should be online', () => {
      service = new DefaultOfflineService();

      expect(service.status).toBe('online');
      expect(window.addEventListener).toHaveBeenCalledWith('online', expect.any(Function));
      expect(window.addEventListener).toHaveBeenCalledWith('offline', expect.any(Function));
      expect(global.setInterval).toHaveBeenCalledWith(expect.any(Function), PeriodicCheckIntervalInMs);
    });

    it('when navigator not online, should be offline', () => {
      // @ts-ignore
      global.window.navigator['onLine'] = false;

      service = new DefaultOfflineService();

      expect(service.status).toBe('offline');
    });
  });

  describe('onStatusChanged', () => {
    let onlineCallback: (event: Event) => void;
    let offlineCallback: (event: Event) => void;

    beforeEach(() => {
      vi.mocked(window.addEventListener).mockImplementation((event: string, callback: (event: Event) => void) => {
        if (event === 'online') {
          onlineCallback = callback;
        } else if (event === 'offline') {
          offlineCallback = callback;
        }
      });
      service = new DefaultOfflineService();
    });

    it('should register callback and return unsubscribe function', () => {
      const callback = vi.fn();

      const unsubscribe = service.onStatusChanged(callback);

      expect(typeof unsubscribe).toBe('function');
      expect(callback).not.toHaveBeenCalled();
    });

    it('when status changes, call callback', () => {
      const callback = vi.fn();
      service.onStatusChanged(callback);

      // Simulate offline event
      offlineCallback(new Event('offline'));

      expect(callback).toHaveBeenCalledWith('offline');
    });

    it('when status changes, call multiple callbacks', () => {
      const callback1 = vi.fn();
      const callback2 = vi.fn();
      service.onStatusChanged(callback1);
      service.onStatusChanged(callback2);

      // Simulate offline event
      offlineCallback(new Event('offline'));

      expect(callback1).toHaveBeenCalledWith('offline');
      expect(callback2).toHaveBeenCalledWith('offline');
    });

    it('when status does not change, not call callback', () => {
      const callback = vi.fn();
      service.onStatusChanged(callback);

      // Service starts online, simulate another online event
      onlineCallback(new Event('online'));

      expect(callback).not.toHaveBeenCalled();
    });

    it('when unsubscribe function is called, remove callback', () => {
      const callback = vi.fn();
      const unsubscribe = service.onStatusChanged(callback);

      unsubscribe();

      // Simulate offline event
      offlineCallback(new Event('offline'));

      expect(callback).not.toHaveBeenCalled();
    });

    it('should handle unsubscribing non-existent callback gracefully', () => {
      const callback = vi.fn();
      const unsubscribe = service.onStatusChanged(callback);

      // Call unsubscribe twice
      unsubscribe();
      unsubscribe();

      // Should not throw error
      expect(() => offlineCallback(new Event('offline'))).not.toThrow();
    });
  });

  describe('network event handling', () => {
    let onlineCallback: (event: Event) => void;
    let offlineCallback: (event: Event) => void;

    beforeEach(() => {
      vi.mocked(window.addEventListener).mockImplementation((event: string, callback: (event: Event) => void) => {
        if (event === 'online') {
          onlineCallback = callback;
        } else if (event === 'offline') {
          offlineCallback = callback;
        }
      });
      service = new DefaultOfflineService();
    });

    it('when online event is fired, update status to online', () => {
      // Start offline
      offlineCallback(new Event('offline'));
      expect(service.status).toBe('offline');

      // Go online
      onlineCallback(new Event('online'));
      expect(service.status).toBe('online');
    });

    it('when offline event is fired, update status to offline', () => {
      // Start online (default)
      expect(service.status).toBe('online');

      // Go offline
      offlineCallback(new Event('offline'));
      expect(service.status).toBe('offline');
    });
  });

  describe('periodic health check', () => {
    // @ts-ignore
    let onlineCallback: (event: Event) => void;
    let offlineCallback: (event: Event) => void;

    beforeEach(() => {
      vi.mocked(window.addEventListener).mockImplementation((event: string, callback: (event: Event) => void) => {
        if (event === 'online') {
          onlineCallback = callback;
        } else if (event === 'offline') {
          offlineCallback = callback;
        }
      });
      service = new DefaultOfflineService();
    });

    it('should set up periodic health check with interval', () =>
      expect(global.setInterval).toHaveBeenCalledWith(expect.any(Function), PeriodicCheckIntervalInMs));

    it('when health check succeeds, update status to online', async () => {
      // @ts-ignore
      vi.mocked(global.fetch).mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({})
      });

      // Start offline
      offlineCallback(new Event('offline'));
      expect(service.status).toBe('offline');

      // Execute periodic check
      await intervalCallback();

      expect(service.status).toBe('online');
      expect(global.fetch).toHaveBeenCalledWith('/api/health', {
        method: 'GET',
        cache: 'no-cache',
        signal: expect.any(Object)
      });
      expect(global.AbortSignal.timeout).toHaveBeenCalledWith(5000);
    });

    it('when health check succeeds, update status to offline', async () => {
      vi.mocked(global.fetch).mockRejectedValue(new Error('Network error'));

      // Start online
      expect(service.status).toBe('online');

      // Execute periodic check
      await intervalCallback();

      expect(service.status).toBe('offline');
    });

    it('when health check throws error, update status to offline', async () => {
      vi.mocked(global.fetch).mockRejectedValueOnce(new Error('Network error'));

      // Start online
      expect(service.status).toBe('online');

      // Execute periodic check
      await intervalCallback();

      expect(service.status).toBe('offline');
    });

    it('when health check confirms current online status, not change status', async () => {
      const callback = vi.fn();
      service.onStatusChanged(callback);

      // @ts-ignore
      vi.mocked(global.fetch).mockResolvedValueOnce({
        ok: true,
        json: () => Promise.resolve({})
      });

      // Start online
      expect(service.status).toBe('online');

      // Execute periodic check
      await intervalCallback();

      expect(service.status).toBe('online');
      expect(callback).not.toHaveBeenCalled(); // Status didn't change
    });

    it('when health check confirms current offline status, not change status', async () => {
      const callback = vi.fn();
      service.onStatusChanged(callback);

      vi.mocked(global.fetch).mockRejectedValueOnce(new Error('Network error'));

      // Start offline
      offlineCallback(new Event('offline'));
      expect(service.status).toBe('offline');
      callback.mockClear(); // Clear the call from status change

      // Execute periodic check
      await intervalCallback();

      expect(service.status).toBe('offline');
      expect(callback).not.toHaveBeenCalled(); // Status didn't change
    });
  });
});
