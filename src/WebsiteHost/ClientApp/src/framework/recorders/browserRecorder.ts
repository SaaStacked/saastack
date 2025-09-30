import { recordCrash, recordPageView, recordTrace, recordUse } from '../api/websiteHost';
import { Recorder, SeverityLevel } from '../recorder';


const SkipDebugTracesInTestingOnly: boolean = false;

interface BrowserRecorderOptions {
  skipDebugTracesInTestingOnly: boolean;
}

export abstract class BrowserRecorder implements Recorder {
  private readonly skipDebugTracesInTestingOnly: boolean = SkipDebugTracesInTestingOnly;

  protected constructor(options?: BrowserRecorderOptions) {
    this.skipDebugTracesInTestingOnly = options ? options.skipDebugTracesInTestingOnly : SkipDebugTracesInTestingOnly;
  }

  crash(error: Error, message?: string): void {
    if (window.isTestingOnly) {
      console.error(error, `SaaStack: Crash: ${message}: ${error.message}, ${error.stack}`);
    }
    recordCrash({
      body: {
        message: `SaaStack: Crash: ${message}: ${error.message}, ${error.stack}`
      }
    });
  }

  trace(message: string, severityLevel: SeverityLevel, args?: any): void {
    if (window.isTestingOnly) {
      if (severityLevel === SeverityLevel.Error) {
        if (args) {
          console.error(`SaaStack: TraceError: ${message}`, args);
        } else {
          console.error(`SaaStack: TraceError: ${message}`);
        }
      } else if (severityLevel === SeverityLevel.Warning) {
        if (args) {
          console.warn(`SaaStack: TraceWarning: ${message}`, args);
        } else {
          console.warn(`SaaStack: TraceWarning: ${message}`);
        }
      } else if (severityLevel === SeverityLevel.Information) {
        if (args) {
          console.info(`SaaStack: TraceInformation: ${message}`, args);
        } else {
          console.info(`SaaStack: TraceInformation: ${message}`);
        }
      } else {
        if (args) {
          console.log(`SaaStack: TraceDebug: ${message}`, args);
        } else {
          console.log(`SaaStack: TraceDebug: ${message}`);
        }
      }

      if (severityLevel === SeverityLevel.Debug && this.skipDebugTracesInTestingOnly) {
        return;
      }
    }

    recordTrace({
      body: {
        arguments: args ?? {},
        level: severityLevel.toString(),
        messageTemplate: message
      }
    });
  }

  traceDebug(message: string, args?: any): void {
    this.trace(message, SeverityLevel.Debug, args);
  }

  traceInformation(message: string, args?: any): void {
    this.trace(message, SeverityLevel.Information, args);
  }

  trackPageView(path: string): void {
    recordPageView({
      body: {
        path
      }
    });

    if (window.isTestingOnly) {
      console.log(`SaaStack: PageView: ${path}`);
    }
  }

  trackUsage(eventName: string, additional: { [val: string]: any } | undefined): void {
    recordUse({
      body: {
        eventName,
        additional
      }
    });

    if (window.isTestingOnly) {
      console.log(`SaaStack: Track:${eventName}, with: ${additional}`);
    }
  }
}
