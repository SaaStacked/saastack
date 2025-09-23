import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import React from 'react';
import { I18nextProvider } from 'react-i18next';
import { MemoryRouter } from 'react-router-dom';
import i18n from '../i18n';
import { OfflineServiceProvider } from '../providers/OfflineServiceContext.tsx';
import { IOfflineService } from '../services/IOfflineService.ts';
import { CurrentUserProvider } from './CurrentUserContext';
import { ThemeProvider } from './ThemeContext.tsx';


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
