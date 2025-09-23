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

export class AzureRecorder extends BrowserRecorder {
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

  trace(message: string, severityLevel: SeverityLevel): void {
    super.trace(message, severityLevel);
    if (!window.isTestingOnly) {
      appInsights.trackTrace({
        message,
        severityLevel: toAISeverity(severityLevel)
      });
    }
  }

  traceDebug(message: string): void {
    super.traceDebug(message);
    if (!window.isTestingOnly) {
      appInsights.trackTrace({
        message,
        severityLevel: AISeverityLevel.Verbose
      });
    }
  }

  traceInformation(message: string): void {
    super.traceInformation(message);
    if (!window.isTestingOnly) {
      appInsights.trackTrace({
        message,
        severityLevel: AISeverityLevel.Information
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
