import { AwsRecorder } from './recorders/awsRecorder';
import { AzureRecorder } from './recorders/azureRecorder';
import { NoOpRecorder } from './recorders/noOpRecorder';

const IgnoreTraceDebugInProduction: boolean = true;

export interface Recorder {
  crash: (error: Error, message?: string) => void;
  trace: (message: string, severityLevel: SeverityLevel, args?: any) => void;
  traceDebug: (message: string, args?: any) => void;
  traceInformation: (message: string, args?: any) => void;
  trackPageView: (path: string) => void;
  trackUsage: (eventName: string, additional?: { [index: string]: any }) => void;
}

export const enum SeverityLevel {
  Debug = 'Debug',
  Information = 'Information',
  Warning = 'Warning',
  Error = 'Error'
}

class LazyLoadingRecorder implements Recorder {
  private recorder: Recorder | undefined = undefined;

  crash(error: Error, message?: string): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.crash(error, message);
  }

  trace(message: string, severityLevel: SeverityLevel, args?: any): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.trace(message, severityLevel, args);
  }

  traceDebug(message: string, args?: any): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.traceDebug(message, args);
  }

  traceInformation(message: string, args?: any): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.traceInformation(message, args);
  }

  trackPageView(path: string): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.trackPageView(path);
  }

  trackUsage(eventName: string, additional?: { [val: string]: any } | undefined): void {
    this.ensureUnderlyingRecorder();
    this.recorder?.trackUsage(eventName, additional);
  }

  private ensureUnderlyingRecorder() {
    if (this.recorder !== undefined) {
      return;
    }

    if (window.isHostedOn === 'AZURE') {
      this.recorder = new AzureRecorder({ ignoreDebug: IgnoreTraceDebugInProduction });
      return;
    }

    if (window.isHostedOn === 'AWS') {
      this.recorder = new AwsRecorder({ ignoreDebug: IgnoreTraceDebugInProduction });
      return;
    }

    this.recorder = new NoOpRecorder();
  }
}

export const recorder = new LazyLoadingRecorder();
