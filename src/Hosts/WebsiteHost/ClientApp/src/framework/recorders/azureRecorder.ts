import { SeverityLevel as AISeverityLevel, ApplicationInsights } from '@microsoft/applicationinsights-web';
import { SeverityLevel } from '../recorder';
import { BrowserRecorder } from './browserRecorder';

const appInsightsKey = import.meta.env.VITE_APPLICATIONINSIGHTS_CONNECTIONSTRING;
const applicationInsightsEnabled = import.meta.env.MODE === 'production';

if (applicationInsightsEnabled && !appInsightsKey) {
  console.error('SaaStack: Application Insights instrumentation key is not configured for production');
}

const appInsights = new ApplicationInsights({
  config: {
    connectionString: import.meta.env.VITE_APPLICATIONINSIGHTS_CONNECTIONSTRING
  }
});

if (applicationInsightsEnabled) {
  appInsights.loadAppInsights();
}

interface AzureRecorderOptions {
  ignoreDebug?: boolean;
}

export class AzureRecorder extends BrowserRecorder {
  private readonly ignoreDebug: boolean;

  constructor(options: AzureRecorderOptions = {}) {
    super();
    this.ignoreDebug = options.ignoreDebug ?? false;
  }

  crash(error: Error, message?: string): void {
    super.crash(error, message);
    if (!window.isTestingOnly) {
      appInsights.trackException({
        exception: error,
        severityLevel: AISeverityLevel.Error,
        properties: message ? { message } : undefined
      });
    }
  }

  trace(message: string, severityLevel: SeverityLevel, args?: any): void {
    super.trace(message, severityLevel, args);
    if (!window.isTestingOnly) {
      appInsights.trackTrace({
        message,
        severityLevel: toAISeverity(severityLevel),
        properties: args
      });
    }
  }

  traceDebug(message: string, args?: any): void {
    super.traceDebug(message, args);
    if (!window.isTestingOnly) {
      if (this.ignoreDebug) {
        return;
      }

      appInsights.trackTrace({
        message,
        severityLevel: AISeverityLevel.Verbose,
        properties: args
      });
    }
  }

  traceInformation(message: string, args?: any): void {
    super.traceInformation(message, args);
    if (!window.isTestingOnly) {
      appInsights.trackTrace({
        message,
        severityLevel: AISeverityLevel.Information,
        properties: args
      });
    }
  }

  trackPageView(path: string): void {
    super.trackPageView(path);
    if (!window.isTestingOnly) {
      appInsights.trackPageView({
        name: path
      });
    }
  }

  trackUsage(eventName: string, additional: { [val: string]: any } | undefined): void {
    super.trackUsage(eventName, additional);
    if (!window.isTestingOnly) {
      appInsights.trackMetric({ name: eventName, average: 1 });
    }
  }
}

function toAISeverity(severityLevel: SeverityLevel): AISeverityLevel {
  switch (severityLevel) {
    case SeverityLevel.Debug:
      return AISeverityLevel.Verbose;
    case SeverityLevel.Information:
      return AISeverityLevel.Information;
    case SeverityLevel.Warning:
      return AISeverityLevel.Warning;
    case SeverityLevel.Error:
      return AISeverityLevel.Error;

    default:
      return AISeverityLevel.Critical;
  }
}
