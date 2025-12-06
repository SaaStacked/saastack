import { afterAll, beforeAll, beforeEach, vi } from 'vitest';


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

vi.mock('react-i18next', async (importActual) => {
  const actualImpl = await importActual<typeof import('react-i18next')>();
  return {
    ...actualImpl,
    useTranslation: () => ({
      t: (key: string) => key
    })
  };
});

// we need to save the original objects for later to not affect tests from other files
const ogLocation = global.window.location;
const ogNavigator = global.window.navigator;

beforeAll(() => {
  process.env.VITE_WEBSITEHOSTBASEURL = 'https://abaseurl';
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
  // @ts-ignore
  global.window.location = ogLocation;
  // noinspection JSConstantReassignment
  global.window.navigator = ogNavigator;
});
