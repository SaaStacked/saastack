import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { createRoot } from 'react-dom/client';
import { CurrentUserProvider } from './actions/identity/CurrentUserContext.tsx';
import { initializeApiClient } from './api';
import App from './App.tsx';
import { recorder } from './recorder';
import { OfflineServiceProvider } from './services/OfflineServiceContext.tsx';


initializeApiClient();

if (window.isTestingOnly) {
  recorder.traceInformation(`TESTINGONLY is enabled, and this app is HOSTED-ON: ${window.isHostedOn}`);
}

export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: false,
      refetchOnWindowFocus: false,
      staleTime: 10000 // prevents fetch from refreshing more than once every 10 seconds
    },
    mutations: {
      retry: false
    }
  }
});

const container = document.getElementById('root');
const root = createRoot(container!);
root.render(
  <QueryClientProvider client={queryClient}>
    <OfflineServiceProvider>
      <CurrentUserProvider>
        <App />
      </CurrentUserProvider>
    </OfflineServiceProvider>
  </QueryClientProvider>
);
