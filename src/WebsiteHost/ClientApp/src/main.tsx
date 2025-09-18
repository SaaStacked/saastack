import React from 'react';
import './i18n';
import { createRoot } from 'react-dom/client';
import { initializeApiClient } from './api';
import App from './App.tsx';
import { AppProviders } from './providers/AppProviders';
import { recorder } from './recorder';
import './main.css';
import { BrowserRouter } from 'react-router-dom';


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
