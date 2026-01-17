import { QueryClient } from '@tanstack/react-query';
import { render } from '@testing-library/react';
import * as React from 'react';
import { AppProviders } from '../providers/AppProviders';
import { IOfflineService } from '../services/IOfflineService.ts';

interface TestAppProvidersProps {
  children?: React.ReactNode;
  queryClient?: QueryClient;
  offlineService?: IOfflineService;
  initialTestingEntries?: string[];
}

export function TestingProviders({
  queryClient,
  offlineService,
  children,
  initialTestingEntries = undefined
}: TestAppProvidersProps) {
  const queryClient1 = queryClient
    ? queryClient
    : new QueryClient({
        defaultOptions: {
          queries: { retry: false },
          mutations: { retry: false }
        }
      });
  return (
    <AppProviders
      queryClient={queryClient1}
      offlineService={offlineService}
      isTestingOnly={true}
      initialTestingEntries={initialTestingEntries}
      children={children}
    ></AppProviders>
  );
}

export function renderWithTestingProviders(children: React.ReactElement, initialTestingEntries?: string[]) {
  return render(<TestingProviders initialTestingEntries={initialTestingEntries}>{children}</TestingProviders>);
}
