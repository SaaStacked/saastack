import { createRoot } from 'react-dom/client';
import { recorder } from './recorder';
import { initializeApiClient } from './api';
import App from './App.tsx';

initializeApiClient();

if (window.isTestingOnly) {
  recorder.traceInformation(`TESTINGONLY is enabled, and this app is HOSTED-ON: ${window.isHostedOn}`);
}

recorder.trackPageView(window.location.pathname);

const container = document.getElementById('root');
const root = createRoot(container!);
root.render(<App />);
