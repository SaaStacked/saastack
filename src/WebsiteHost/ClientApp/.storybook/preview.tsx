import type { Preview } from '@storybook/react';
import '../src/main.css';
import { initialize, mswLoader } from 'msw-storybook-addon';
import { handlers } from '../src/testing/Storybook/msw-handlers';
import { StorybookProviders } from '../src/testing/Storybook/StorybookProviders';


// Initialize MSW addon
initialize();

const preview: Preview = {
  parameters: {
    controls: {
      matchers: {
        color: /(background|color)$/i,
        date: /Date$/i
      }
    },
    backgrounds: {
      default: 'dark',
      values: [
        {
          name: 'light',
          value: '#ffffff'
        },
        {
          name: 'dark',
          value: '#333333'
        },
        {
          name: 'gray',
          value: '#f8fafc'
        }
      ]
    },
    msw: {
      handlers
    }
  },
  decorators: [
    (Story) => (
      <StorybookProviders>
        <Story />
      </StorybookProviders>
    )
  ],
  loaders: [mswLoader]
};

export default preview;
