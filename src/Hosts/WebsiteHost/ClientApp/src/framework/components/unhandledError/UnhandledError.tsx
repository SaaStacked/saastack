import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { ErrorResponse } from '../../actions/Actions.ts';
import { ProblemDetails } from '../../api/apiHost1';
import { createComponentId } from '../Components.ts';
import Tag from '../tag/Tag.tsx';


interface UnhandledErrorProps {
  id?: string;
  error?: ErrorResponse;
}

// Creates an inline error message to display an unexpected error, and its technical details
export default function UnhandledError({ id, error }: UnhandledErrorProps) {
  if (!error) {
    return null;
  }
  const { t: translate } = useTranslation();
  const [isExpanded, setIsExpanded] = useState(false);
  const [isStackTraceExpanded, setIsStackTraceExpanded] = useState(false);
  const errorAsAny = error.data as any;
  const errorAsJavaScriptError = error.data instanceof Error ? error.data : undefined;
  const errorAsProblemDetails = error.data as ProblemDetails;
  const statusCode = error.response
    ? error.response.status
    : (errorAsProblemDetails?.status ?? errorAsAny?.status ?? 'unknown');
  const errorCode = errorAsJavaScriptError
    ? undefined
    : (error.response?.statusText ?? errorAsProblemDetails?.title ?? errorAsAny?.error);
  const errorMessage =
    errorAsJavaScriptError?.message ?? errorAsProblemDetails?.detail ?? errorAsAny?.message ?? 'unknown';
  const moreDetails = errorAsProblemDetails
    ? errorAsProblemDetails.errors
      ? (errorAsProblemDetails.errors as any[]).map((error: any) => error.reason)
      : []
    : [];
  const clientStackTrace = errorAsJavaScriptError ? errorAsJavaScriptError.stack : undefined;
  const serverStackTrace =
    errorAsProblemDetails && errorAsProblemDetails.exception ? (errorAsProblemDetails.exception as string) : undefined;

  const componentId = createComponentId('unhandled_error', id);
  return (
    <div
      className="border border-error dark:border-error-700 bg-error-300/20 dark:bg-error-700/20 rounded-lg p-2 max-w-4xl"
      data-testid={componentId}
      id={componentId}
    >
      <div className="mb-4">
        <div className="flex items-center mb-2">
          <div className="w-10 h-10 text-error-600 mr-3">
            <svg fill="currentColor" viewBox="0 0 20 20">
              <path
                fillRule="evenodd"
                d="M18 10a8 8 0 11-16 0 8 8 0 0116 0zm-7 4a1 1 0 11-2 0 1 1 0 012 0zm-1-9a1 1 0 00-1 1v4a1 1 0 102 0V6a1 1 0 00-1-1z"
                clipRule="evenodd"
              />
            </svg>
          </div>
          <h1 className="text-base font-semibold text-error-600 dark:text-error-300">
            {translate('components.unhandled_error.title')}
          </h1>
        </div>
        <p className="mb-4 text-xs text-error-800">{translate('components.unhandled_error.subtitle')}</p>
      </div>

      {!isExpanded ? (
        <button
          className="flex items-center text-sm text-neutral-600 dark:text-neutral-400 hover:text-neutral-800 dark:hover:text-neutral-200"
          type="button"
          onClick={() => setIsExpanded(true)}
        >
          <span className="mr-1 text-xs">+</span>
          <span className="text-xs">{translate('components.unhandled_error.links.details')}</span>
        </button>
      ) : (
        <div
          className="bg-white dark:bg-neutral-800 border border-error-300 dark:border-error-700 rounded-md p-4"
          id={`${componentId}_details`}
          data-testid={`${componentId}_details`}
        >
          <h4 className="text-xs text-neutral-900 dark:text-neutral-100 mb-3">
            {translate('components.unhandled_error.links.details')}
          </h4>
          <div className="space-y-1">
            <div className="flex flex-wrap items-center gap-2">
              <span className="text-xs text-neutral-700 dark:text-neutral-300">
                {translate('components.unhandled_error.status')}:
              </span>
              <Tag className="text-sm" label="HTTP" color="error">
                <span className="ml-1 mr-1" data-testid={`${componentId}_details_statusCode`}>
                  {statusCode}
                </span>
              </Tag>
              {errorCode && (
                <>
                  <span className="text-xs text-neutral-600 dark:text-neutral-400">-</span>
                  <span
                    className="text-xs text-neutral-900 dark:text-neutral-100 font-medium"
                    data-testid={`${componentId}_details_errorCode`}
                  >
                    {errorCode}
                  </span>
                </>
              )}
            </div>

            {errorMessage && (
              <div className="flex items-center gap-2">
                <span className="text-xs text-neutral-700 dark:text-neutral-300">
                  {translate('components.unhandled_error.message')}:
                </span>
                <span
                  className="inline-flex items-center px-2 py-1 rounded text-xs bg-neutral-100 dark:bg-neutral-700 text-neutral-800 dark:text-neutral-200"
                  data-testid={`${componentId}_details_errorMessage`}
                >
                  {errorMessage}
                </span>
              </div>
            )}
            {moreDetails.length > 0 && (
              <div className="flex items-center gap-2">
                <span className="text-xs text-neutral-700 dark:text-neutral-300">
                  {translate('components.unhandled_error.more_details')}:
                </span>
                {moreDetails.map((detail, index) => (
                  <span
                    key={index}
                    className="inline-flex items-center px-2 py-1 rounded text-xs font-mono bg-neutral-50 dark:bg-neutral-700 text-neutral-800 dark:text-neutral-200"
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
                  className="flex items-center text-xs text-neutral-600 dark:text-neutral-400 hover:text-neutral-800 dark:hover:text-neutral-200"
                  type="button"
                  onClick={() => setIsStackTraceExpanded(true)}
                >
                  <span className="mr-1">+</span>
                  <span>{translate('components.unhandled_error.links.more_details')}</span>
                </button>
              ) : (
                <div className="mt-1">
                  {clientStackTrace && (
                    <div className="mt-4">
                      <span className="text-xs text-neutral-700 dark:text-neutral-300 block mb-2">
                        {translate('components.unhandled_error.client_stack_trace')}:
                      </span>
                      <pre
                        className="text-xs font-mono text-neutral-600 dark:text-neutral-300 bg-neutral-50 dark:bg-neutral-800 border border-neutral-200 dark:border-neutral-600 rounded p-3 overflow-x-auto whitespace-pre-wrap break-words max-h-40 overflow-y-auto"
                        data-testid={`${componentId}_details_clientStackTrace`}
                      >
                        {clientStackTrace}
                      </pre>
                    </div>
                  )}
                  {serverStackTrace && (
                    <div className="mt-4">
                      <span className="text-xs text-neutral-700 dark:text-neutral-300 block mb-2">
                        {translate('components.unhandled_error.server_stack_trace')}:
                      </span>
                      <pre
                        className="text-xs font-mono text-neutral-600 dark:text-neutral-300 bg-neutral-50 dark:bg-neutral-800 border border-neutral-200 dark:border-neutral-600 rounded p-3 overflow-x-auto whitespace-pre-wrap break-words max-h-40 overflow-y-auto"
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
