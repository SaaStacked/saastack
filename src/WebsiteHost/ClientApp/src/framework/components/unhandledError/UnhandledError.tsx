import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { AxiosError } from 'axios';
import { createComponentId } from '../Components.ts';


interface UnhandledErrorProps {
  id?: string;
  error?: AxiosError;
}

// Creates an inline error message to display an unexpected error, and its technical details
export default function UnhandledError({ id, error }: UnhandledErrorProps) {
  if (!error) {
    return null;
  }
  const { t: translate } = useTranslation();
  const [isExpanded, setIsExpanded] = useState(false);
  const [isStackTraceExpanded, setIsStackTraceExpanded] = useState(false);
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
      className="border border-red-300 dark:border-red-600 bg-red-50 dark:bg-red-900/20 rounded-lg p-2 max-w-4xl"
      data-testid={componentId}
      id={componentId}
    >
      <div className="mb-4">
        <div className="flex items-center mb-2">
          <div className="w-10 h-10 text-red-600 dark:text-red-400 mr-3">
            <svg fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z"
                clipRule="evenodd"
              />
            </svg>
          </div>
          <h1 className="text-lg font-bold text-red-800 dark:text-red-200">
            {translate('components.unhandled_error.title')}
          </h1>
        </div>
        <p className="text-sm text-red-700 dark:text-red-300 mb-4">
          {translate('components.unhandled_error.subtitle')}
        </p>
      </div>

      {!isExpanded ? (
        <button
          type="button"
          onClick={() => setIsExpanded(true)}
          className="flex items-center text-sm text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200"
        >
          <span className="mr-1 text-xs">+</span>
          <span className="text-xs">{translate('components.unhandled_error.links.details')}</span>
        </button>
      ) : (
        <div
          className="bg-white dark:bg-gray-800 border border-red-200 dark:border-red-700 rounded-md p-4"
          id={`${componentId}_details`}
          data-testid={`${componentId}_details`}
        >
          <h4 className="text-xs text-gray-900 dark:text-gray-100 mb-3">
            {translate('components.unhandled_error.links.details')}
          </h4>
          <div className="space-y-1">
            <div className="flex flex-wrap items-center gap-2">
              <span className="text-xs text-gray-700 dark:text-gray-300">
                {translate('components.unhandled_error.status')}:
              </span>
              <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs bg-red-100 dark:bg-red-900/30 text-red-800 dark:text-red-200">
                HTTP{' '}
                <span className="ml-1" data-testid={`${componentId}_details_statusCode`}>
                  {statusCode}
                </span>
              </span>
              {errorCode && (
                <>
                  <span className="text-xs text-gray-600 dark:text-gray-400">-</span>
                  <span
                    className="text-xs text-gray-900 dark:text-gray-100 font-medium"
                    data-testid={`${componentId}_details_errorCode`}
                  >
                    {errorCode}
                  </span>
                </>
              )}
            </div>

            {errorMessage && (
              <div className="flex items-center gap-2">
                <span className="text-xs text-gray-700 dark:text-gray-300">
                  {translate('components.unhandled_error.message')}:
                </span>
                <span
                  className="inline-flex items-center px-2 py-1 rounded text-xs bg-gray-100 dark:bg-gray-700 text-gray-800 dark:text-gray-200"
                  data-testid={`${componentId}_details_errorMessage`}
                >
                  {errorMessage}
                </span>
              </div>
            )}
            {moreDetails.length > 0 && (
              <div className="flex items-center gap-2">
                <span className="text-xs text-gray-700 dark:text-gray-300">
                  {translate('components.unhandled_error.more_details')}:
                </span>
                {moreDetails.map((detail, index) => (
                  <span
                    key={index}
                    className="inline-flex items-center px-2 py-1 rounded text-xs font-mono bg-gray-50 dark:bg-gray-700 text-gray-800 dark:text-gray-200"
                    data-testid={`${componentId}_details_moreDetails`}
                  >
                    {detail}
                  </span>
                ))}
              </div>
            )}

            <div>
              {(clientStackTrace || serverStackTrace) && !isStackTraceExpanded ? (
                <button
                  type="button"
                  onClick={() => setIsStackTraceExpanded(true)}
                  className="flex items-center text-xs text-gray-600 dark:text-gray-400 hover:text-gray-800 dark:hover:text-gray-200"
                >
                  <span className="mr-1">+</span>
                  <span>{translate('components.unhandled_error.links.more_details')}</span>
                </button>
              ) : (
                <div className="mt-1">
                  {clientStackTrace && (
                    <div className="mt-4">
                      <span className="text-xs text-gray-700 dark:text-gray-300 block mb-2">
                        {translate('components.unhandled_error.client_stack_trace')}:
                      </span>
                      <pre
                        className="text-xs font-mono text-gray-600 dark:text-gray-300 bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded p-3 overflow-x-auto whitespace-pre-wrap break-words max-h-40 overflow-y-auto"
                        data-testid={`${componentId}_details_clientStackTrace`}
                      >
                        {clientStackTrace}
                      </pre>
                    </div>
                  )}
                  {serverStackTrace && (
                    <div className="mt-4">
                      <span className="text-xs text-gray-700 dark:text-gray-300 block mb-2">
                        {translate('components.unhandled_error.server_stack_trace')}:
                      </span>
                      <pre
                        className="text-xs font-mono text-gray-600 dark:text-gray-300 bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-600 rounded p-3 overflow-x-auto whitespace-pre-wrap break-words max-h-40 overflow-y-auto"
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
      )}
    </div>
  );
}
