import * as React from 'react';
import { ToastContainer } from 'react-toastify';
import { TestingProviders } from '../TestingProviders';

interface StorybookProvidersProps {
  children?: React.ReactNode;
  initialTestingEntries?: string[];
}

export function StorybookProviders({ children, initialTestingEntries = undefined }: StorybookProvidersProps) {
  return (
    <TestingProviders initialTestingEntries={initialTestingEntries}>
      {children}
      <ToastContainer
        position="top-right"
        autoClose={5000}
        hideProgressBar={false}
        newestOnTop={false}
        closeOnClick
        rtl={false}
        pauseOnFocusLoss
        draggable
        pauseOnHover
      />
    </TestingProviders>
  );
}
