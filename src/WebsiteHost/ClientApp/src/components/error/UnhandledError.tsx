import { AxiosError } from 'axios';

interface UnhandledErrorProps {
  id: string;
  error?: AxiosError;
}

// Creates an inline error message to display an unexpected error, and its details
export default function UnhandledError({ id, error }: UnhandledErrorProps) {
  if (!error) {
    return null;
  }

  const statusCode = error.response?.status;
  const statusText = error.message;
  const errorCode = error.code;
  const stackTrace = error.stack;

  const componentId = id + '_unhandled_error';
  return (
    <div
      id={componentId}
      data-testid={componentId}
      className="border border-red-300 bg-red-50 rounded-lg p-6 max-w-4xl"
    >
      <div className="mb-4">
        <div className="flex items-center mb-2">
          <div className="w-6 h-6 text-red-600 mr-3">
            <svg fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z"
                clipRule="evenodd"
              />
            </svg>
          </div>
          <h3 className="text-lg font-semibold text-red-800">Unexpected Error</h3>
        </div>
        <p className="text-red-700 mb-4">Sorry! This error was completely unexpected at this time!</p>
      </div>

      <div
        id={`${componentId}_details`}
        data-testid={`${componentId}_details`}
        className="bg-white border border-red-200 rounded-md p-4"
      >
        <h4 className="text-sm font-medium text-gray-900 mb-3">Error Details</h4>
        <div className="space-y-2">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-sm font-medium text-gray-700">Status:</span>
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-red-100 text-red-800">
              HTTP{' '}
              <span data-testid={`${componentId}_details_statusCode`} className="ml-1">
                {statusCode}
              </span>
            </span>
            <span className="text-sm text-gray-600">-</span>
            <span data-testid={`${componentId}_details_statusText`} className="text-sm text-gray-900 font-medium">
              {statusText}
            </span>
          </div>

          {errorCode && (
            <div className="flex items-center gap-2">
              <span className="text-sm font-medium text-gray-700">Error Code:</span>
              <span
                data-testid={`${componentId}_details_errorCode`}
                className="inline-flex items-center px-2 py-1 rounded text-xs font-mono bg-gray-100 text-gray-800"
              >
                {errorCode}
              </span>
            </div>
          )}

          {stackTrace && (
            <div className="mt-4">
              <span className="text-sm font-medium text-gray-700 block mb-2">Stack Trace:</span>
              <pre
                data-testid={`${componentId}_details_stackTrace`}
                className="text-xs font-mono text-gray-600 bg-gray-50 border border-gray-200 rounded p-3 overflow-x-auto whitespace-pre-wrap break-words max-h-40 overflow-y-auto"
              >
                {stackTrace}
              </pre>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
