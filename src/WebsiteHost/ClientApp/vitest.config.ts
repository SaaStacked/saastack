/// <reference types="vitest" />

import { mergeConfig } from 'vite';
import viteConfig from './vite.config';

export default mergeConfig(viteConfig, {
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/testing/setup.ts'],
    testTimeout: 20000
  }
});
