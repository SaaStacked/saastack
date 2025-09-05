import { useState } from 'react';
import { AxiosError } from 'axios';
import { createComponentId } from '../Components.ts';


interface UnhandledErrorProps {
  id?: string;
  error?: AxiosError;
}

// Creates an inline error message to display an unexpected error, and its details
export default function UnhandledError({ id, error }: UnhandledErrorProps) {
  if (!error) {
    return null;
  }
  const [isExpanded, setIsExpanded] = useState(false);
  const responseData = error.response?.data as any;
  const statusCode = error.response ? error.response?.status : error.status;
  const errorCode = error.response ? error.response.statusText : error.code;
  const errorMessage = error.response && responseData ? responseData.detail : error.message;
  const moreDetails =
    error.response && responseData
      ? responseData.errors
        ? (responseData.errors as any[]).map((error: any) => error.reason)
        : []
      : [];
  const clientStackTrace = error.stack;
  const serverStackTrace = error.response && responseData ? responseData.exception : undefined;

  const componentId = createComponentId('unhandled_error', id);
  return (
    <div
      className="border border-red-300 bg-red-50 rounded-lg p-6 max-w-4xl"
      data-testid={componentId}
      id={componentId}
    >
      <div className="mb-4">
        <div className="flex items-center mb-2">
          <div className="w-12 h-12 text-red-600 mr-3">
            <svg fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z"
                clipRule="evenodd"
              />
            </svg>
          </div>
          <h1 className="text-xl font-bold text-red-800">Unexpected Error</h1>
        </div>
        <p className="text-red-700 font-medium mb-4">Oh no! We did not expect an error like this!</p>
      </div>

      <div
        className="bg-white border border-red-200 rounded-md p-4"
        id={`${componentId}_details`}
        data-testid={`${componentId}_details`}
      >
        <h4 className="text-sm font-medium text-gray-900 mb-3">Error Details</h4>
        <div className="space-y-2">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-sm font-medium text-gray-700">Status:</span>
            <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-sm font-medium bg-red-100 text-red-800">
              HTTP{' '}
              <span className="ml-1" data-testid={`${componentId}_details_statusCode`}>
                {statusCode}
              </span>
            </span>
            {errorCode && (
              <>
                <span className="text-sm text-gray-600">-</span>
                <span className="text-sm text-gray-900 font-medium" data-testid={`${componentId}_details_errorCode`}>
                  {errorCode}
                </span>
              </>
            )}
          </div>

          {errorMessage && (
            <div className="flex items-center gap-2">
              <span className="text-sm font-medium text-gray-700">Message:</span>
              <span
                className="inline-flex items-center px-2 py-1 rounded text-sm bg-gray-100 text-gray-800"
                data-testid={`${componentId}_details_errorMessage`}
              >
                {errorMessage}
              </span>
            </div>
          )}
          {moreDetails.length > 0 && (
            <div className="flex items-center gap-2">
              <span className="text-sm font-medium text-gray-700">More Details:</span>
              {moreDetails.length > 0 ? (
                moreDetails.map((detail) => (
                  <span
                    className="inline-flex items-center px-2 py-1 rounded text-xs font-mono bg-gray-50 text-gray-800"
                    data-testid={`${componentId}_details_moreDetails`}
                  >
                    {detail}
                  </span>
                ))
              ) : (
                <span
                  className="inline-flex items-center px-2 py-1 rounded text-xs font-mono bg-gray-50 text-gray-800"
                  data-testid={`${componentId}_details_moreDetails`}
                >
                  {moreDetails[0]}
                </span>
              )}
            </div>
          )}

          <div>
            {(clientStackTrace || serverStackTrace) && !isExpanded ? (
              <button
                type="button"
                onClick={() => setIsExpanded(true)}
                className="flex items-center text-sm text-gray-600 hover:text-gray-800"
              >
                <span className="mr-1">+</span>
                Show Technical Details
              </button>
            ) : (
              <div className="mt-1">
                {clientStackTrace && (
                  <div className="mt-4">
                    <span className="text-sm font-medium text-gray-700 block mb-2">Browser Stack Trace:</span>
                    <pre
                      className="text-xs font-mono text-gray-600 bg-gray-50 border border-gray-200 rounded p-3 overflow-x-auto whitespace-pre-wrap break-words max-h-40 overflow-y-auto"
                      data-testid={`${componentId}_details_clientStackTrace`}
                    >
                      {clientStackTrace}
                    </pre>
                  </div>
                )}
                {serverStackTrace && (
                  <div className="mt-4">
                    <span className="text-sm font-medium text-gray-700 block mb-2">Server Stack Trace:</span>
                    <pre
                      className="text-xs font-mono text-gray-600 bg-gray-50 border border-gray-200 rounded p-3 overflow-x-auto whitespace-pre-wrap break-words max-h-40 overflow-y-auto"
                      data-testid={`${componentId}_details_serverStackTrace`}
                    >
                      {serverStackTrace}
                    </pre>
                  </div>
                )}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
}
