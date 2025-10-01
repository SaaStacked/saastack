import { useEffect, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useOfflineService } from '../../providers/OfflineServiceContext.tsx';
import { toClasses } from '../Components.ts';


export const animationDurationInMs = 2000;

// Creates a full screen banner when the browser is offline
// Message appears automatically at the top of the page, and disappears automatically
export function OfflineBanner() {
  const { t: translate } = useTranslation();
  const offlineService = useOfflineService();
  const [isOffline, setIsOffline] = useState(offlineService.status === 'offline');
  const [shouldRender, setShouldRender] = useState(isOffline);
  const [isVisible, setIsVisible] = useState(false);

  useEffect(() => {
    const unsubscribe = offlineService.onStatusChanged((status) => {
      const offline = status === 'offline';
      setIsOffline(offline);

      if (offline) {
        setShouldRender(true);
        // Trigger animation after component renders
        setTimeout(() => setIsVisible(true), 10);
      } else {
        setIsVisible(false);
        // Delay unmounting to allow exit animation
        setTimeout(() => setShouldRender(false), animationDurationInMs);
      }
    });

    // Set the initial state
    if (isOffline) {
      setTimeout(() => setIsVisible(true), 10);
    }

    return unsubscribe;
  }, [offlineService, isOffline]);

  if (!shouldRender) {
    return null;
  }

  const baseClasses =
    'relative top-0 left-0 right-0 z-50 bg-red-500 dark:bg-red-800 p-1 rounded-b-xl transform transition-transform duration-1000 ease-in-out';
  const translateClasses = isVisible ? 'translate-y-0' : '-translate-y-full';
  const classes = toClasses([baseClasses, translateClasses]);

  return (
    <div className={classes}>
      <div className="container mx-auto max-w-4xl text-sm text-right">
        <span className="text-gray-200 dark:text-gray-300">{translate('components.offline.error')}</span>
        <span className="hidden sm:inline text-gray-200 dark:text-gray-300">
          :&nbsp;{translate('components.offline.reason')}&nbsp;&nbsp;
        </span>
      </div>
    </div>
  );
}
