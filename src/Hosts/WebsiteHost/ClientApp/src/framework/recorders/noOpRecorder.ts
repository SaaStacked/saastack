import { Recorder, SeverityLevel } from '../recorder';

export class NoOpRecorder implements Recorder {
  // @ts-ignore
  crash(error: Error, message?: string): void {
    // Does nothing by definition
  }

  // @ts-ignore
  trace(message: string, severityLevel: SeverityLevel, args?: any): void {
    // Does nothing by definition
  }

  // @ts-ignore
  traceDebug(message: string, args?: any): void {
    // Does nothing by definition
  }

  // @ts-ignore
  traceInformation(message: string, args?: any): void {
    // Does nothing by definition
  }

  // @ts-ignore
  trackPageView(path: string): void {
    // Does nothing by definition
  }

  // @ts-ignore
  trackUsage(eventName: string, additional: { [val: string]: any } | undefined): void {
    // Does nothing by definition
  }
}
