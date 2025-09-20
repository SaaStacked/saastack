import React from 'react';
import './framework/i18n';
import { createRoot } from 'react-dom/client';
import App from './App.tsx';
import './main.css';
import { BrowserRouter } from 'react-router-dom';
import { initializeApiClient } from './framework/api';
import { AppProviders } from './framework/providers/AppProviders.tsx';
import { recorder } from './framework/recorder.ts';


initializeApiClient();

if (window.isTestingOnly) {
  recorder.traceInformation(`TESTINGONLY is enabled, and this app is HOSTED-ON: ${window.isHostedOn}`);
}

const container = document.getElementById('root');
const root = createRoot(container!);
root.render(
  <AppProviders>
    <React.StrictMode>
      <BrowserRouter>
        <App />
      </BrowserRouter>
    </React.StrictMode>
  </AppProviders>
);
