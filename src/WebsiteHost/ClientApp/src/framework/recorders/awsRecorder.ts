import { SeverityLevel } from '../recorder';
import { BrowserRecorder } from './browserRecorder';


interface AwsRecorderOptions {
  ignoreDebug?: boolean;
}

export class AwsRecorder extends BrowserRecorder {
  private readonly ignoreDebug: boolean;

  constructor(options: AwsRecorderOptions = {}) {
    super();
    this.ignoreDebug = options.ignoreDebug ?? false;
  }

  crash(error: Error, message?: string): void {
    super.crash(error, message);
    if (!window.isTestingOnly) {
      //TODO: send to AWS CloudWatch
    }
  }

  trace(message: string, severityLevel: SeverityLevel, args?: any): void {
    super.trace(message, severityLevel, args);
    if (!window.isTestingOnly) {
      //TODO: send to AWS CloudWatch
    }
  }

  traceDebug(message: string, args?: any): void {
    super.traceDebug(message, args);
    if (!window.isTestingOnly) {
      if (this.ignoreDebug) {
        return;
      }
      //TODO: send to AWS CloudWatch
    }
  }

  traceInformation(message: string, args?: any): void {
    super.traceInformation(message, args);
    if (!window.isTestingOnly) {
      //TODO: send to AWS CloudWatch
    }
  }

  trackPageView(path: string): void {
    super.trackPageView(path);
    if (!window.isTestingOnly) {
      //TODO: send to AWS CloudWatch
    }
  }

  trackUsage(eventName: string, additional: { [val: string]: any } | undefined): void {
    super.trackUsage(eventName, additional);
    if (!window.isTestingOnly) {
      //TODO: send to AWS CloudWatch
    }
  }
}
