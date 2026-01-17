import { useQuery } from '@tanstack/react-query';
import { useCallback, useEffect, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useOfflineService } from '../providers/OfflineServiceContext.tsx';
import { recorder, SeverityLevel } from '../recorder.ts';
import { ActionResult, ApiResponse, executeRequest, handleRequestError, modifyRequestData } from './Actions.ts';
import useApiErrorState from './ApiErrorState.ts';

export interface ActionQueryConfiguration<
  TRequestData = any,
  ExpectedErrorCode extends string = '',
  TResponse = any,
  TTransformedResponse = any
> {
  // The generated Fetch endpoint we need to call
  request: (requestData: TRequestData) => Promise<ApiResponse<TResponse>>;
  // Whether the request is tenanted or not
  isTenanted?: boolean;
  // What to do in the case of a successful response
  onSuccess?: (requestData: TRequestData, response: TTransformedResponse, statusCode: number, headers: Headers) => void;
  // The transformation function to apply to the response
  transform: (result: TResponse) => TTransformedResponse;
  // What kind of known errors are we expecting to handle ourselves
  passThroughErrors?: Record<number, ExpectedErrorCode>;
  // The cache key to use to store the response
  cacheKey: readonly unknown[];
}

// Use this hook for calling @hey-api (fetch) generated endpoints for GET or SEARCH.
// Use the useActionCommand hook for POST, PUT, PATCH or DELETE endpoints
// Supports automatic OrganizationId population for isTenanted requests
// Supports monitoring of requests for displaying loading indicators
// Supports monitoring of expected errors versus unexpected errors
// Supports monitoring of online/offline status
export function useActionQuery<
  TRequestData = any,
  TResponse = any,
  TTransformedResponse = any,
  ExpectedErrorCode extends string = any
>(
  configuration: ActionQueryConfiguration<TRequestData, ExpectedErrorCode, TResponse, TTransformedResponse>
): ActionResult<TRequestData, ExpectedErrorCode, TTransformedResponse> {
  const { t: translate } = useTranslation();
  const { request, passThroughErrors, cacheKey, transform: onTransform, onSuccess } = configuration;

  const { onError: handleError, expectedError, unexpectedError, clearErrors } = useApiErrorState(passThroughErrors);

  const offlineService = useOfflineService();
  let isOnline = offlineService && offlineService.status === 'online';

  const [currentRequestData, setCurrentRequestData] = useState<TRequestData | undefined>();
  const currentRequestDataRef = useRef<TRequestData | undefined>(currentRequestData ?? ({} as TRequestData));

  const {
    refetch,
    data: response,
    isSuccess,
    isFetching,
    isPending,
    isError,
    error: queryError
  } = useQuery({
    enabled: false, // Prevents automatic execution, execute it manually by calling refetch
    queryKey: cacheKey, // HACK: This cache key is immutable. We cannot use the specific request data values in the key, so we cannot vary the cache by request data
    queryFn: async () => {
      isOnline = offlineService && offlineService.status === 'online';
      if (isOnline) {
        const requestData = currentRequestDataRef.current ?? ({} as TRequestData);
        return await executeRequest(request, requestData);
      } else {
        recorder.trace('QueryCommand: Cannot execute query when browser is offline', SeverityLevel.Warning);
        throw new Error(translate('actions.errors.offline'));
      }
    },
    select: useCallback(
      (apiResponse: ApiResponse<TResponse>): ApiResponse<TTransformedResponse> => {
        recorder.traceDebug('QueryCommand: Query returned success');
        if (apiResponse === undefined || apiResponse === null) {
          recorder.traceDebug('QueryCommand: Query returned no data!');
          return {
            data: {} as TTransformedResponse,
            error: undefined,
            request: {} as Request,
            response: { ok: true, status: 200 } as Response
          } as ApiResponse<TTransformedResponse>;
        }

        if (apiResponse.error != undefined) {
          return apiResponse as ApiResponse<TTransformedResponse>;
        }

        let response: ApiResponse<TTransformedResponse>;
        if (onTransform) {
          const transformed = onTransform(apiResponse.data ?? ({} as TResponse));

          response = {
            data: transformed,
            error: undefined,
            request: apiResponse.request,
            response: apiResponse.response
          } as ApiResponse<TTransformedResponse>;
        } else {
          response = {
            data: apiResponse.data,
            error: undefined,
            request: apiResponse.request,
            response: apiResponse.response
          } as ApiResponse<TTransformedResponse>;
        }

        if (onSuccess) {
          const requestData = currentRequestDataRef.current ?? ({} as TRequestData);
          onSuccess(
            requestData,
            response.data ?? ({} as TTransformedResponse),
            response.response.status,
            response.response.headers
          );
        }

        return response;
      },
      [onTransform, onSuccess]
    ),
    retry: false,
    refetchOnWindowFocus: false,
    refetchOnMount: false,
    refetchOnReconnect: false,
    refetchInterval: false,
    refetchIntervalInBackground: false,
    throwOnError: (_error: Error, _query) => false
  });

  useEffect(() => {
    if (!isError) {
      clearErrors();
    } else {
      handleRequestError(queryError, handleError);
    }
  }, [isError, queryError]);

  const executeCallback = useCallback(
    (
      requestData?: TRequestData,
      {
        onSuccess
      }: {
        onSuccess?: (params: {
          requestData?: TRequestData;
          response: TTransformedResponse;
          statusCode: number;
          headers: Headers;
        }) => void;
      } = {}
    ) => {
      let submittedRequestData: TRequestData = modifyRequestData(requestData, configuration.isTenanted);
      setCurrentRequestData(submittedRequestData);
      currentRequestDataRef.current = submittedRequestData;

      recorder.traceDebug('QueryCommand: Executing query, with request: {Request}', { request: submittedRequestData });
      refetch({
        throwOnError: false
      })
        .then((result) => {
          // Make sure we don't call onSuccess if there is an error, given that throwOnError is always false
          if (result.isError && result.error != undefined) {
            recorder.traceDebug('QueryCommand: Query returned error: {Error}', { result });
            return;
          }

          if (result.data == undefined) {
            recorder.traceDebug('QueryCommand: Query returned no data!');
            return;
          }

          const apiResponse = result.data as ApiResponse<TTransformedResponse>;
          if (onSuccess) {
            onSuccess({
              requestData,
              response: apiResponse.data ?? ({} as TTransformedResponse),
              statusCode: apiResponse.response.status,
              headers: apiResponse.response.headers
            });
          }
        })
        .catch((error) => {
          // we should never get here, since throwOnError is always set to false
          recorder.trace('useActionQuery: Failed to refetch query action', SeverityLevel.Warning);
          throw error;
        });
    },
    [refetch, configuration.isTenanted]
  );

  const isExecuting = (isPending || isFetching) && isError == false && isSuccess == false;
  const isCompleted = isPending || isFetching ? undefined : isSuccess ? true : isError ? false : undefined;
  const variables = currentRequestDataRef.current ?? ({} as TRequestData);
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
