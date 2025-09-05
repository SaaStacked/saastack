import { IOfflineService } from './IOfflineService';


export class DefaultOfflineService implements IOfflineService {
  private _callbacks: Array<(status: 'online' | 'offline') => void> = [];

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

  private startPeriodicCheck(): void {
    setInterval(async () => {
      try {
        // We deliberately use fetch with no caching
        const response = await fetch('/api/health', {
          method: 'GET',
          cache: 'no-cache',
          signal: AbortSignal.timeout(5000)
        });
        this.updateStatus(response.ok ? 'online' : 'offline');
      } catch {
        this.updateStatus('offline');
      }
    }, 30000); //every 30 seconds
  }
}
