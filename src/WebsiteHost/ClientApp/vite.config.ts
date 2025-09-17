import fs from 'fs';
import path, { resolve } from 'path';
import react from '@vitejs/plugin-react';
import tailwindcss from '@tailwindcss/vite';
import type { Plugin } from 'vite';
import { defineConfig } from 'vite';

function generateBundledFiles(): Plugin {
  return {
    name: 'bundle-file-generator',
    writeBundle(options, bundle) {
      if (process.env.NODE_ENV === 'production' || options.dir?.includes('wwwroot')) {
        const jsFiles: string[] = [];
        const cssFiles: string[] = [];

        console.log(`📦 [bundle-file-generator]`);
        console.log(`Bundling files:`);

        Object.keys(bundle).forEach((fileName) => {
          const file = bundle[fileName];

          if (file.type === 'chunk' && file.isEntry) {
            jsFiles.push(fileName);
          } else if (file.type === 'asset' && fileName.endsWith('.css')) {
            cssFiles.push(fileName);
          }
          console.log(`   '${fileName}'`);
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
          const manifestContent = JSON.stringify(manifest, null, 2);
          fs.writeFileSync(manifestPath, manifestContent);

          console.log(`Regenerated JSApp manifest`);
          console.log(`Content at ${manifestPath}:`);
          console.log(`${manifestContent}`);
        }
      }
    }
  };
}

export default defineConfig({
  build: {
    outDir: path.resolve(__dirname, '..', 'wwwroot'),
    emptyOutDir: true,
    copyPublicDir: true,
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
    tailwindcss(),
    react()
  ],
  esbuild: {
    target: 'esnext'
  },
  resolve: {
    extensions: ['.tsx', '.ts', '.jsx', '.js', '.json'],
    alias: {
      '@': path.resolve(__dirname, './src')
    }
  }
});
