import type { Meta, StoryObj } from '@storybook/react';
import { useCallback, useMemo, useState } from 'react';
import { OfflineServiceContext } from '../../providers/OfflineServiceContext.tsx';
import { IOfflineService } from '../../services/IOfflineService.ts';
import { OfflineBanner } from './OfflineBanner';

const meta: Meta<typeof OfflineBanner> = {
  title: 'Components/OfflineBanner',
  component: OfflineBanner,
  parameters: {
    layout: 'fullscreen'
  },
  tags: ['autodocs']
};

export default meta;
type Story = StoryObj<typeof meta>;

export const GoOfflineOnline: Story = {
  render: () => {
    const [status, setStatus] = useState<'online' | 'offline'>('online');
    const [callbacks, setCallbacks] = useState<Array<(status: 'online' | 'offline') => void>>([]);

    const onStatusChanged = useCallback((callback: (status: 'online' | 'offline') => void) => {
      setCallbacks((prev) => [...prev, callback]);
      return () => setCallbacks((prev) => prev.filter((cb) => cb !== callback));
    }, []);

    const mockService: IOfflineService = useMemo(
      () => ({
        status,
        onStatusChanged
      }),
      [status, onStatusChanged]
    );

    const toggleStatus = () => {
      const newStatus = status === 'online' ? 'offline' : 'online';
      setStatus(newStatus);
      callbacks.forEach((cb) => cb(newStatus));
    };

    return (
      <OfflineServiceContext.Provider value={{ offlineService: mockService }}>
        <div className="min-h-screen bg-neutral-50 dark:bg-neutral-900">
          <OfflineBanner />
          <div className="p-8">
            <button
              onClick={toggleStatus}
              className={`px-4 py-2 rounded mb-4 text-white ${
                status === 'online' ? 'bg-red-500 hover:bg-red-600' : 'bg-green-500 hover:bg-green-600'
              }`}
            >
              {status === 'online' ? 'Go Offline' : 'Go Online'}
            </button>
          </div>
        </div>
      </OfflineServiceContext.Provider>
    );
  }
};
