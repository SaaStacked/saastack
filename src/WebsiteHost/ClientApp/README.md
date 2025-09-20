# WebsiteHost JS App

## Packaging & Bundling

We use [Vite](https://vitejs.dev/) for fast development and building, which outputs JavaScript files to the `wwwroot` folder, with some other static assets live (e.g. SEO images, and SEO configuration) that are stored in the `public` folder. See the `../.gitignore` file for more details.

### Bundling

Vite treats 'development' builds and 'production' builds very differently. It also supports a hot-loading dev server.
We support a mixed mode environment, where you can use the dev server (without hot-loading), or not use the dev server. It is your choice.

* When you use `npm run dev` it starts a Vite dev server on port 5173, which serves the JavaScript files from the `src` folder.
* When you use `npm run build` it compiles and bundles the JavaScript files from the `src` folder into a single bundle in the `wwwroot` folder.

We are deliberately rendering the `Index.html` page server-side (see: `HomeController.cs`) for security purposes, thus we need to load the correct JavaScript and CSS files into `Index.html` at runtime both locally and in production.

* In 'development' mode, we need to reference the Typescript from the `src` folder of the dev server at: `http://localhost:5173/src/main.tsx` and `http://localhost:5173/src/main.css`.
* In 'production' mode, we need to reference the vite compiled JavaScript bundle from the `https://app.saastack.com/BSaMLVRv.bundle.js` folder (which is located in the `wwwroot` folder).


> Everytime the `npm run build` command is run (as is expected in CI/CD for 'production' builds), the `jsapp.build.json` file is updated with the latest bundle file names.

> However, when `npm run dev` is executed this file is not updated!

When the website is run, the `IJsAppBundler` service is used to load the appropriate JavaScript files.
It will first check for the existence of the Vite dev server at port 5173, and if it is running, it will load the JavaScript classes from there.
Otherwise, it will load the JavaScript bundle from the `wwwroot` folder, given the paths from the `jsapp.build.json` file. 
This data is then injected into the `Index.html` page.

## API Definitions

We use AXIOS to call the APIs in the BEFFE.

We generate all AXIOS services automatically for you by examining the BEFFE API and the BACKEND APIs, and then generate the services for you.

You can update those definitions at any time by running `npm run update:apis` to keep the backend and frontend in sync.

> For this to script to work properly, you must run both the BEFFE and the BACKEND APIs locally on your local machine, so that the OpenAPI swagger endpoint is reachable. You do this in Rider, by running the `AllHosts` compound configuration (runs the `ApiHost1` server, the `WebsiteHost` server and the `TestingStubApiHost`) exposing all the API endpoints and BEEFE.

## Testing

Most of this code is covered in unit tests, where possible.

We use [Vitest](https://vitest.dev/) for unit testing, which provides a Jest-compatible API with better performance and native TypeScript support.

### Running Tests

- `npm test` - Run tests locally and continuously in watch mode
- `npm run test:ci` - Run tests (for CI/CD) and generate code coverage reports

### Test Configuration

- Tests run in a `jsdom` environment to simulate browser behavior
- Global test utilities are available in the `src/testing` folder.
- Test files should use `.spec.ts` extensions, and be alongside the code they are testing.

## Environment Configuration

Configuration is defined in `.env` files.

* When running locally, `npm run dev` will use the `.env` file, and will overwrite any values defined in `.env.local` file.
* When building for production, `npm run build` will use the `.env.deploy` file.

There is a special variable called: `window.isTestingOnly` which you SHOULD use to conditionally execute code that is only relevant in local or CI testing environments.

## Website Design

The site has been designed to be as vanilla as possible, using very standard practices readily adopted by most SaaS products.

We anticipate that for any derivative product, these things we be changed or extended significantly to suit the needs of the product.

We have avoided requiring any unnecessary dependencies
For the dependencies that we have taken, we have configured them in very standard ways, that can easily be changed.

We have structured the code in a similar way to the backend, in that the web pages themselves are organized according to subdomain. Shared components are organized differently.

## Recording

For all tracing, crash reporting, page views and product usage metrics, you MUST use the `IRecorder` interface.
This data is relayed to the BEFFE to be persisted in centralised logs along with all other telemetry, and the traces, audits and usages form the backend API.

> You can see this recorded data appear locally, in the output console of the `TestingSubApiHost: TestingStubApiHost-Development` project, when running the `AllHosts` compound configuration.

Since this kind of sensitive data is passed to the BEFFE (from a browser) on the same domain that the JavaScript was loaded from, users cannot block this critical data from being collected with Ad Blockers or private browser sessions in their browsers. This kind of traffic is safe for them.
This is a major advantage over using 3rd party JavaScript SDKs and libraries to relay information to 3rd party systems, which often use cookies to track users, which can be easily blocked by browsers. We strongly recommend NOT using those kinds of JavaScript SDKs if you can avoid it. 

For tracing and error reporting, DO NOT use `console.log()`, `console.warn()` or `console.error()` statements in the JS App code - except in tests. Instead, MUST use the `IRecorder` interface to log messages. and these messages will be relayed to the BEFFE to be persisted in centralised logs along with all other telemetry, and the traces, audits and usages form the backend API.

> In `window.isTestingOnly === true` the configured `IRecorder` will echo all calls to `IRecorder` out to the browser console any way. And in production builds, these messages will never be visible to the end user. Their browser console in their developer tools should remain empty.

We recommend the following practices, using the `IRecorder`:
1. Use `recorder.traceInformation()` for all diagnostic messages of interest to the operation of the system.
2. Use `recorder.crash()` where you code detects an exceptional error case.
3. Use `recorder.trackUsage()` to capture all product metrics. All API calls are already tracked in by the backend API, but the backend API cannot track what the user does in the UI (mouse movements, clicks, navigation etc.), nor anything else that the user does in the UI that does not result in an API call. Add appropriate `trackUsage()` calls to capture those UI events. The BEFFE will take care of capturing the user agent properties and other browser metadata.
4. For page views, use the `recorder.trackPageView()` method. This method is already wired into the React router, so as long as all routes are defined in React router, you should have nothing to do here.  

# StoryBook

Every component has one or more storybook stories, to maintain a gallery of components, and their various states/flavors. Those files are located next to the component files, and have the suffix `.stories.tsx`.

The story book should be built regularly, especially after changing components, using the common `npm run build-storybook`
Once built you can run the storybook on your local machine and interact with it, and verify that the components are working as expected.

To run the storybook, use the `npm run storybook` command.

## Components

There are a number of components already present, and you will likely want to keep these for your product. And only change the layout/styling of them.

There are a set of very basic components, that can be used anywhere, and there are a specific set of components for dealing with interactive forms in the `src/framework/components/form` folder. These are specialized to work with the `<Form/>` component, combined with Actions (`src/framework/actions/ActionCommand.ts` and `src/framework/actions/ActionQuery.ts`)

## Pages

Pages are organized according to subdomain.

Page URLs are defined in `src/App.tsx` and if those change, you may need to update the values in the `WebsiteUiService` class to follow.

The relevant pages from the `Identity` subdomain are all implemented fully. You will likely want to keep these for your product. And only change the layout/styling of them.

Pages from sample subdomain (`Cars` and `Bookings`) are also implemented, but you will likely want to delete these for your product. They are here only as examples to follow.

### Tailwind Styling

We are using Tailwind CSS v4 to style the site.

Tailwind, unlike other CSS frameworks provides a very low-level set of utility classes that can be combined to create any design. This allows us to make the site as customizable as possible. This has advantages and disadvantages, like any CSS system out there. This can of course be ripped and replaced with your preferred CSS system.

We have tried to be as consistent as possible, where possible with TailWind classes, but tried to avoid too much refinement.

To add your own custom classes to Tailwind, utilize the options in `src/main.css`.

## Localization

We have set this site to be localised from the start. The user has the ability to change their language at any time, and the site will be displayed in that language.

The only language currently supported is English (i.e. `en`), but it is trivial to add more languages, following the guidance from the `i18next` documentation.

The language files are located in the `public/locales` folder.