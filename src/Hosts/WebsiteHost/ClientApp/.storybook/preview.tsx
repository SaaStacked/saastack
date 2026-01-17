import type { Preview } from '@storybook/react';
import '../src/main.css';
import * as React from 'react';
import { initialize, mswLoader } from 'msw-storybook-addon';
import { handlers } from '../src/framework/testing/Storybook/msw-handlers';
import { StorybookProviders } from '../src/framework/testing/Storybook/StorybookProviders';

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
      default: 'light',
      values: [
        {
          name: 'light',
          value: '#E5E7EB' /* gray-200 */
        },
        {
          name: 'dark',
          value: '#101828' /* gray-900 */
        }
      ]
    },
    msw: {
      handlers
    }
  },
  globalTypes: {
    theme: {
      description: 'Global theme for components',
      defaultValue: 'light',
      toolbar: {
        title: 'Theme',
        icon: 'paintbrush',
        items: [
          { value: 'light', title: 'Light', icon: 'sun' },
          { value: 'dark', title: 'Dark', icon: 'moon' }
        ],
        dynamicTitle: true
      }
    }
  },
  decorators: [
    (Story, context) => {
      const theme = context.globals.theme || 'dark';

      // Apply theme to document root for proper CSS cascade
      React.useEffect(() => {
        const root = document.documentElement;
        root.classList.remove('light', 'dark');
        root.classList.add(theme);
      }, [theme]);

      return (
        <div className={theme}>
          <StorybookProviders>
            <Story />
          </StorybookProviders>
        </div>
      );
    }
  ],
  loaders: [mswLoader]
};

export default preview;
