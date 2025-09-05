import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { MemoryRouter } from 'react-router-dom';
import { IOfflineService } from '../services/IOfflineService.ts';
import { OfflineServiceProvider } from '../services/OfflineServiceContext';
import { CurrentUserProvider } from './CurrentUserContext';

interface AppProvidersProps {
  children: React.ReactNode;
  queryClient?: QueryClient;
  offlineService?: IOfflineService;
  isTestingOnly?: boolean;
  initialTestingEntries?: string[];
}

export function AppProviders({
  children,
  queryClient: customQueryClient,
  offlineService,
  isTestingOnly = false,
  initialTestingEntries = undefined
}: AppProvidersProps) {
  const queryClient =
    customQueryClient ||
    new QueryClient({
      defaultOptions: {
        queries: {
          retry: false,
          refetchOnWindowFocus: false,
          staleTime: 10000 // prevents fetch from refreshing more than once every 10 seconds
        },
        mutations: { retry: false }
      }
    });

  const content = (
    <QueryClientProvider client={queryClient}>
      <OfflineServiceProvider offlineService={offlineService}>
        <CurrentUserProvider>{children}</CurrentUserProvider>
      </OfflineServiceProvider>
    </QueryClientProvider>
  );

  if (isTestingOnly) {
    return <MemoryRouter initialEntries={initialTestingEntries}>{content}</MemoryRouter>;
  }

  return content;
}
