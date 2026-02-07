import type { StorybookConfig } from '@storybook/react-vite';
import { mergeConfig } from 'vite';


const config: StorybookConfig = {
  stories: ['../src/**/*.stories.@(js|jsx|mjs|ts|tsx)'],
  staticDirs: ['../public'],
  addons: ['msw-storybook-addon'],
  core: {
    builder: '@storybook/builder-vite'
  },
  framework: {
    name: '@storybook/react-vite',
    options: {}
  },
  typescript: {
    check: false,
    reactDocgen: 'react-docgen-typescript',
    reactDocgenTypescriptOptions: {
      shouldExtractLiteralValuesFromEnum: true,
      propFilter: (prop) => (prop.parent ? !/node_modules/.test(prop.parent.fileName) : true)
    }
  },
  async viteFinal(config) {
    return mergeConfig(config, {
      plugins: [
        // @ts-ignore
        (await import('@tailwindcss/vite')).default()
      ]
    });
  }
};

export default config;
