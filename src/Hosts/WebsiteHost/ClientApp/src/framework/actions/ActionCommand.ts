import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useOfflineService } from '../providers/OfflineServiceContext.tsx';
import { recorder, SeverityLevel } from '../recorder.ts';
import { ActionResult, ApiResponse, executeRequest, handleRequestError, modifyRequestData } from './Actions.ts';
import useApiErrorState from './ApiErrorState.ts';

export interface ActionCommandConfiguration<
  TRequestData = any,
  ExpectedErrorCode extends string = '',
  TResponse = any
> {
  // The generated Fetch endpoint we need to call
  request: (requestData: TRequestData, throwOnError?: boolean) => Promise<ApiResponse<TResponse>>;
  // Whether the request is tenanted or not
  isTenanted?: boolean;
  // What to do in the case of a successful response
  onSuccess?: (requestData: TRequestData, response: TResponse, statusCode: number, headers: Headers) => void;
  // What kind of known errors are we expecting to handle ourselves
  passThroughErrors?: Record<number, ExpectedErrorCode>;
  // The keys in the request cache that we want to invalidate, in the case of successful response
  invalidateCacheKeys?: readonly unknown[];
}

// Use this hook for calling @hey-api (fetch) generated endpoints for POST, PUT, PATCH or DELETE.
// Use the useActionQuery hook for GET or SEARCH endpoints
// Supports automatic OrganizationId population for isTenanted requests
// Supports monitoring of requests for displaying loading indicators
// Supports monitoring of expected errors versus unexpected errors
// Supports monitoring of online/offline status
export function useActionCommand<TRequestData = any, TResponse = any, ExpectedErrorCode extends string = any>(
  configuration: ActionCommandConfiguration<TRequestData, ExpectedErrorCode, TResponse>
): ActionResult<TRequestData, ExpectedErrorCode, TResponse> {
  const { t: translate } = useTranslation();
  const { request, passThroughErrors, onSuccess, invalidateCacheKeys } = configuration;

  const queryClient = useQueryClient();
  const { onError: handleError, expectedError, unexpectedError, clearErrors } = useApiErrorState(passThroughErrors);

  const offlineService = useOfflineService();
  let isOnline = offlineService && offlineService.status === 'online';

  const {
    mutate,
    data: response,
    isSuccess,
    isError,
    isPending,
    variables
  } = useMutation({
    mutationFn: async (requestData) => {
      isOnline = offlineService && offlineService.status === 'online';
      if (isOnline) {
        return await executeRequest(request, requestData);
      } else {
        recorder.trace('ActionCommand: Cannot execute command when browser is offline', SeverityLevel.Warning);
        throw new Error(translate('actions.errors.offline'));
      }
    },
    onSuccess: (apiResponse: ApiResponse<TResponse>, requestData: TRequestData) => {
      recorder.traceDebug('ActionCommand: Mutation returned success');
      clearErrors();
      if (invalidateCacheKeys) {
        recorder.traceDebug('ActionCommand: clearing cache keys: {Keys}', { invalidateCacheKeys });
        queryClient.invalidateQueries({
          queryKey: invalidateCacheKeys,
          exact: false // we want to support wildcards
        });
      }

      if (onSuccess) {
        onSuccess(
          requestData,
          apiResponse.data ?? ({} as TResponse),
          apiResponse.response.status,
          apiResponse.response.headers
        );
      }
    },
    onError: (error) => handleRequestError(error, handleError),
    throwOnError: (_error: Error) => false,
    retry: false
  });

  const executeCallback = useCallback(
    (
      requestData?: TRequestData,
      {
        onSuccess
      }: {
        onSuccess?: (params: {
          requestData?: TRequestData;
          response: TResponse;
          statusCode: number;
          headers: Headers;
        }) => void;
      } = {}
    ) => {
      let submittedRequestData: TRequestData = modifyRequestData(requestData, configuration.isTenanted);

      recorder.traceDebug('ActionCommand: Executing command, with request', {
        submittedRequestData
      });
      mutate(submittedRequestData, {
        onSuccess: (apiResponse, requestData) => {
          if (onSuccess) {
            onSuccess({
              requestData,
              response: apiResponse.data ?? ({} as TResponse),
              statusCode: apiResponse.response.status,
              headers: apiResponse.response.headers
            });
          }
        }
      });
    },
    [mutate, configuration.isTenanted]
  );

  const isExecuting = isPending && isError == false && isSuccess == false;
  const isCompleted = isPending ? undefined : isSuccess ? true : isError ? false : undefined;
  return {
    execute: executeCallback,
    lastSuccessResponse: response?.data,
    isSuccess: isCompleted,
    lastExpectedError: expectedError,
    lastUnexpectedError: unexpectedError,
    isExecuting,
    isReady: isOnline,
    lastRequestValues: isOnline ? variables : undefined
  };
}
