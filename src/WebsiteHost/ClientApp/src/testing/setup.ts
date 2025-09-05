import { afterAll, beforeAll, beforeEach, vi } from 'vitest';

vi.mock('axios', async (importActual) => {
  const actualImpl = await importActual<typeof import('@hey-api/client-axios')>();

  return {
    ...actualImpl,
    request: vi.fn((config) => Promise.resolve({ config, data: {}, status: 200 }))
  };
});

vi.mock('@hey-api/client-axios', async (importActual) => {
  const actualImpl = await importActual<typeof import('@hey-api/client-axios')>();

  return {
    ...actualImpl,
    createClient: vi.fn().mockImplementation((config) => {
      const clientImpl = actualImpl.createClient(config);
      return {
        ...clientImpl,
        get: vi.fn((config) => Promise.resolve({ config, data: {}, status: 200 })),
        post: vi.fn((config) => Promise.resolve({ config, data: {}, status: 201 })),
        put: vi.fn((config) => Promise.resolve({ config, data: {}, status: 202 })),
        delete: vi.fn((config) => Promise.resolve({ config, data: {}, status: 204 }))
      };
    })
  };
});

vi.mock('../recorder.ts', async (importActual) => {
  const actualImpl = await importActual<typeof import('../recorder.ts')>();

  return {
    ...actualImpl,
    crash: vi.fn(),
    trace: vi.fn(),
    traceDebug: vi.fn(),
    traceInformation: vi.fn(),
    trackPageView: vi.fn(),
    trackUsage: vi.fn()
  };
});

// we need to save the original objects for later to not affect tests from other files
const ogLocation = global.window.location;
const ogNavigator = global.window.navigator;

beforeAll(() => {
  process.env.VITE_WEBSITEHOSTBASEURL = 'abaseurl';
  vi.spyOn(document, 'querySelector').mockImplementation((selector) => {
    if (selector == "meta[name='csrf-token']") {
      return {
        getAttribute: vi.fn().mockReturnValue('acsrftoken')
      } as unknown as Element;
    }

    return null;
  });

  // @ts-ignore
  delete global.window.location;
  // noinspection JSConstantReassignment
  delete global.window.navigator;
  // noinspection JSConstantReassignment
  global.window = Object.create(window);
  // @ts-ignore
  global.window.location = { assign: vi.fn() };
  // @ts-ignore
  // noinspection JSConstantReassignment
  global.window.navigator = {
    onLine: true
  };
  global.window.addEventListener = vi.fn();
  global.window.removeEventListener = vi.fn();
});

beforeEach(() => vi.clearAllMocks());

afterAll(() => {
  global.window.location = ogLocation;
  // noinspection JSConstantReassignment
  global.window.navigator = ogNavigator;
});
