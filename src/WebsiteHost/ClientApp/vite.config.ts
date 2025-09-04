import type { Plugin } from 'vite';
import { defineConfig } from 'vite';
import path, { resolve } from 'path';
import react from '@vitejs/plugin-react';
import fs from 'fs';

function generateBundledFiles(): Plugin {
  return {
    name: 'bundle-file-generator',
    writeBundle(options, bundle) {
      if (process.env.NODE_ENV === 'production' || options.dir?.includes('wwwroot')) {
        const jsFiles: string[] = [];
        const cssFiles: string[] = [];

        Object.keys(bundle).forEach((fileName) => {
          const file = bundle[fileName];

          if (file.type === 'chunk' && file.isEntry) {
            jsFiles.push(fileName);
          } else if (file.type === 'asset' && fileName.endsWith('.css')) {
            cssFiles.push(fileName);
          }
        });

        const mainJs = jsFiles.find((f) => f.includes('.bundle.js')) || jsFiles[0];
        const mainCss = cssFiles.find((f) => f.includes('.bundle.css')) || cssFiles[0];

        if (mainJs || mainCss) {
          const manifest = {
            main: {
              js: mainJs || '',
              css: mainCss || ''
            }
          };

          const manifestPath = path.resolve(__dirname, 'jsapp.build.json');
          fs.writeFileSync(manifestPath, JSON.stringify(manifest, null, 2));

          console.log(`📦 [bundle-file-generator] Generated 'jsapp.build.json':`);
          console.log(`   JS: ${mainJs || 'none'}`);
          console.log(`   CSS: ${mainCss || 'none'}`);
        }
      }
    }
  };
}

export default defineConfig({
  build: {
    outDir: path.resolve(__dirname, '..', 'wwwroot'),
    emptyOutDir: true,
    rollupOptions: {
      // Match the port number in ViteJsAppBundler.JsAppEntryPoint
      input: resolve(__dirname, 'src/main.tsx'),
      output: {
        entryFileNames: '[hash].bundle.js',
        chunkFileNames: '[hash].chunk.js',
        assetFileNames: (assetInfo) => {
          // Name CSS files with .bundle.css extension
          if (assetInfo.name?.endsWith('.css')) {
            return '[hash].bundle.css';
          }
          return '[hash].[ext]';
        }
      }
    },

    sourcemap: true,
    target: 'esnext'
  },

  // Match the port number in ViteJsAppBundler.ViteDevServerPort
  server: {
    port: 5173,
    open: false
  },

  plugins: [
    {
      ...generateBundledFiles(),
      apply: 'build'
    },
    react()
  ],
  esbuild: {
    target: 'esnext'
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.jsx', '.js', '.json']
  }
});
