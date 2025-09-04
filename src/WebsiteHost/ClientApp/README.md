# WebsiteHost JS App

## Packaging & Bundling

We use [Vite](https://vitejs.dev/) for fast development and building, which outputs JavaScript files to the `wwwroot` folder, with some other static assets live (e.g. SEO images, and SEO configuration) that are stored in the `public` folder. See the `../.gitignore` file for more details.

### Bundling

Vite treats 'development' builds and 'production' builds differently. It also supports a hot-loading dev server.

We are deliberately rendering the `Index.html` page server-side (see: `HomeController.cs`), and then loading the Vite JavaScript bundle client-side. See: https://vite.dev/guide/backend-integration.html for more details.

In 'development' mode, we need to reference the Typescript from the `src` folder of the dev server at: `http://localhost:5173/src/main.tsx`.
In 'production' mode, we need to reference the vite compiled JavaScript bundle from the `https://app.saastack.com/BSaMLVRv.bundle.js` folder (which is located in the `wwwroot` folder).


Everytime the `npm run build` command is run (as is expected in CI/CD for 'production' builds), the `jsapp.build.json` file is updated with the latest bundle file names.

> However, when `npm run dev` is executed this file is not updated

When the website is run, the `IJsAppBundler` service is used to load the appropriate JavaScript files.
It will first check for the existence of the Vite dev server at port 5173, and if it is running, it will load the JavaScript classes from there.
Otherwise, it will load the JavaScript bundle from the `wwwroot` folder, given the paths from the `jsapp.build.json` file. 
This data is then injected into the `Index.html` page.


## API Definitions

We use AXIOS to call the APIs in the BEFFE.

We generate these services automatically for you, by examining the BEFFE API and the BACKEND APIs, and then generate the services for you.
You can update those at any time by running `npm run update:apis`

For this to work properly you must run both the BEFFE and the BACKEND APIs locally, so that the OpenAPI swagger endpoint is reachable.

## Build Commands

- `npm run build` - Build for production using Vite
- `npm run build:releasefordeploy` - Build for production deployment
- `npm run dev` - Start Vite development server with hot reload, then open the browser at `http://localhost:5173`
- `npm run preview` - Preview production build locally
- `npm run clean` - Remove generated bundle files and manifest
- `npm run update:apis` - Update API client code from OpenAPI specifications

## Testing

We use [Vitest](https://vitest.dev/) for unit testing, which provides a Jest-compatible API with better performance and native TypeScript support.

### Running Tests

- `npm test` - Run tests in watch mode
- `npm run test:run` - Run tests once (for CI/CD)

### Test Configuration

- Tests run in a jsdom environment to simulate browser behavior
- Global test utilities are available (describe, it, expect, etc.)
- Test files should use `.test.ts` or `.spec.ts` extensions

## Environment Configuration

Configuration is defined in `.env` files.

* When running locally, `npm run dev` will use the `.env` file, and will overwrite any values defined in `.env.local` file.
* When building for production, `npm run build` will use the `.env.deploy` file.