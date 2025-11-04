/// <reference types="vitest" />

import { mergeConfig } from 'vite';
import viteConfig from './vite.config';

export default mergeConfig(viteConfig, {
  test: {
    globals: true,
    environment: 'jsdom',
    setupFiles: ['./src/framework/testing/setup.ts'],
    testTimeout: 20000,
    coverage: {
      enabled: false,
      reporter: ['text', 'lcov'],
      exclude: ['src/framework/testing/**/*', 'src/main.tsx'],
      provider: 'v8'
    }
  }
});
