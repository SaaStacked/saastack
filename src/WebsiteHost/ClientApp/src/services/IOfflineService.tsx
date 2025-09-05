export interface IOfflineService {
  readonly status: 'online' | 'offline';

  onStatusChanged(callback: (status: 'online' | 'offline') => void): () => void;
}
