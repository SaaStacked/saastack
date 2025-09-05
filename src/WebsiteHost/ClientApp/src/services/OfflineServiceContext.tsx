import { createContext, useContext } from 'react';
import { DefaultOfflineService } from './DefaultOfflineService';
import { IOfflineService } from './IOfflineService';


interface OfflineServiceProps {
  children: React.ReactNode;
  offlineService?: IOfflineService;
}

interface OfflineServiceContextContent {
  offlineService: IOfflineService;
}

const OfflineServiceContext = createContext<OfflineServiceContextContent | null>(null);

export function OfflineServiceProvider({ children, offlineService = new DefaultOfflineService() }: OfflineServiceProps) {
  return <OfflineServiceContext.Provider value={{ offlineService }}>{children}</OfflineServiceContext.Provider>;
}

export function useOfflineService() {
  const context = useContext(OfflineServiceContext);
  if (!context) {
    throw new Error('useOfflineService must be used within an OfflineServiceProvider');
  }
  return context.offlineService;
}
