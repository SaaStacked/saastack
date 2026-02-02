import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { I18nextProvider } from 'react-i18next';
import { MemoryRouter } from 'react-router-dom';
import i18n from '../i18n';
import { IOfflineService } from '../services/IOfflineService.ts';
import { CurrentUserProvider } from './CurrentUserContext';
import { OfflineServiceProvider } from './OfflineServiceContext.tsx';
import { ThemeProvider } from './ThemeContext.tsx';

export const QueryClientDefaultCacheTimeInMs: number = 1000 * 30; // 30 seconds

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
          staleTime: QueryClientDefaultCacheTimeInMs // all data is automatically invalidated after 10 seconds
        }
      }
    });

  const content = (
    <ThemeProvider>
      <I18nextProvider i18n={i18n}>
        <QueryClientProvider client={queryClient}>
          <OfflineServiceProvider offlineService={offlineService}>
            <CurrentUserProvider>{children}</CurrentUserProvider>
          </OfflineServiceProvider>
        </QueryClientProvider>
      </I18nextProvider>
    </ThemeProvider>
  );

  if (isTestingOnly) {
    return <MemoryRouter initialEntries={initialTestingEntries}>{content}</MemoryRouter>;
  }

  return content;
}
