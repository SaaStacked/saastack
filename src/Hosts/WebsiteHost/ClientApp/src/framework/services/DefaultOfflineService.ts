import { IOfflineService } from './IOfflineService';


export const PeriodicCheckIntervalInMs = 60000; //every minute

export class DefaultOfflineService implements IOfflineService {
  private _callbacks: Array<(status: 'online' | 'offline') => void> = [];
  private _lastFetchTime: number = 0;
  private _isFetching: boolean = false;

  constructor() {
    this.initializeNetworkMonitoring();
  }

  private _status: 'online' | 'offline' = 'online';

  get status(): 'online' | 'offline' {
    return this._status;
  }

  onStatusChanged(callback: (status: 'online' | 'offline') => void): () => void {
    this._callbacks.push(callback);
    return () => {
      const index = this._callbacks.indexOf(callback);
      if (index > -1) {
        this._callbacks.splice(index, 1);
      }
    };
  }

  private initializeNetworkMonitoring(): void {
    window.addEventListener('online', () => this.updateStatus('online'));
    window.addEventListener('offline', () => this.updateStatus('offline'));

    this.updateStatus(window.navigator.onLine ? 'online' : 'offline');
    this.startPeriodicCheck();
  }

  private updateStatus(status: 'online' | 'offline'): void {
    if (this._status !== status) {
      this._status = status;
      this._callbacks.forEach((callback) => callback(status));
    }
  }

  // Checks the health of the API every minute
  // Makes sure that the browser is not just connected to the network, but that the API is actually reachable
  // Runs once every minute, and no more than once a minute, even if requests are made more frequently
  private startPeriodicCheck(): void {
    setInterval(async () => {
      const now = Date.now();
      const oneMinuteAgo = now - 60000;

      // Skip if we're already fetching or last fetch was less than a minute ago
      if (this._isFetching || this._lastFetchTime > oneMinuteAgo) {
        return;
      }

      this._isFetching = true;
      this._lastFetchTime = now;

      try {
        const response = await fetch('/api/health', {
          method: 'GET',
          cache: 'no-cache',
          signal: AbortSignal.timeout(5000)
        });
        this.updateStatus(response.ok ? 'online' : 'offline');
      } catch {
        this.updateStatus('offline');
      } finally {
        this._isFetching = false;
      }
    }, PeriodicCheckIntervalInMs);
  }
}
