import { useQueryClient } from '@tanstack/react-query';
import { useCallback, useRef, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { QueryClientDefaultCacheTimeInMs } from '../providers/AppProviders.tsx';
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
  // The cache keys array to use to store the response
  cacheKey: readonly unknown[] | ((request: TRequestData) => readonly unknown[]);
  // An optional TTL (in ms) to override the default cachePeriod
  cachePeriodMs?: number;
}

// Use this hook for calling @hey-api (fetch) generated endpoints for GET or SEARCH.
// Use the useActionCommand hook for POST, PUT, PATCH or DELETE endpoints
// Supports automatic OrganizationId population for isTenanted requests
// Supports monitoring of requests for displaying loading indicators
// Supports monitoring of expected errors versus unexpected errors
// Supports monitoring of online/offline status
// Supports caching of responses (using cacheKey)
export function useActionQuery<
  TRequestData = any,
  TResponse = any,
  TTransformedResponse = any,
  ExpectedErrorCode extends string = any
>(
  configuration: ActionQueryConfiguration<TRequestData, ExpectedErrorCode, TResponse, TTransformedResponse>
): ActionResult<TRequestData, ExpectedErrorCode, TTransformedResponse> {
  const { t: translate } = useTranslation();
  const queryClient = useQueryClient();
  const { request, passThroughErrors, transform: onTransform, isTenanted, cacheKey, cachePeriodMs } = configuration;

  const { onError: handleError, expectedError, unexpectedError, clearErrors } = useApiErrorState(passThroughErrors);

  const offlineService = useOfflineService();
  const isOnline = offlineService?.status === 'online';

  const requestRef = useRef(request);
  const onTransformRef = useRef(onTransform);
  const handleErrorRef = useRef(handleError);
  const clearErrorsRef = useRef(clearErrors);
  const cacheKeyRef = useRef(cacheKey);
  const cachePeriodMsRef = useRef(cachePeriodMs);
  requestRef.current = request;
  onTransformRef.current = onTransform;
  handleErrorRef.current = handleError;
  clearErrorsRef.current = clearErrors;
  cacheKeyRef.current = cacheKey;
  cachePeriodMsRef.current = cachePeriodMs;

  const [currentRequestData, setCurrentRequestData] = useState<TRequestData | undefined>();
  const currentRequestDataRef = useRef<TRequestData | undefined>(currentRequestData ?? ({} as TRequestData));

  const [response, setResponse] = useState<ApiResponse<TTransformedResponse> | undefined>();
  const [isSuccess, setIsSuccess] = useState<boolean>(false);
  const [isFetching, setIsFetching] = useState<boolean>(false);
  const [isPending, setIsPending] = useState<boolean>(true);
  const [isError, setIsError] = useState<boolean>(false);

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
      let submittedRequestData: TRequestData = modifyRequestData(requestData, isTenanted);
      setCurrentRequestData(submittedRequestData);
      currentRequestDataRef.current = submittedRequestData;

      const calculatedCacheKey =
        typeof cacheKeyRef.current === 'function' ? cacheKeyRef.current(submittedRequestData) : cacheKeyRef.current;

      recorder.traceDebug('QueryCommand: Executing query, with request: {Request}', { request: submittedRequestData });

      setIsPending(false);
      setIsFetching(true);
      clearErrorsRef.current();

      queryClient
        .fetchQuery({
          queryKey: calculatedCacheKey,
          queryFn: async () => {
            if (isOnline) {
              const requestData = currentRequestDataRef.current ?? ({} as TRequestData);
              return await executeRequest(requestRef.current, requestData);
            } else {
              recorder.trace('QueryCommand: Cannot execute query when browser is offline', SeverityLevel.Warning);
              throw new Error(translate('actions.errors.offline'));
            }
          },
          retry: false,
          staleTime: cachePeriodMs ?? QueryClientDefaultCacheTimeInMs
        })
        .then((apiResponse: ApiResponse<TResponse>) => {
          setIsPending(false);
          setIsFetching(false);
          setIsSuccess(true);
          setIsError(false);

          const res = handleResponse<TResponse, TTransformedResponse>(apiResponse, onTransformRef.current);
          setResponse(res);

          if (onSuccess) {
            const requestData = currentRequestDataRef.current ?? ({} as TRequestData);
            onSuccess({
              requestData,
              response: res.data ?? ({} as TTransformedResponse),
              statusCode: res.response.status,
              headers: res.response.headers
            });
          }
          return res;
        })
        .catch((error) => {
          setIsPending(false);
          setIsFetching(false);
          setIsError(true);
          setResponse(undefined);
          handleRequestError(error, handleError);
          recorder.trace('useActionQuery: Failed to refetch query action', SeverityLevel.Warning);
        });
    },
    [isTenanted, queryClient, isOnline, translate]
  );

  const isExecuting = (isPending || isFetching) && !isError && !isSuccess;
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

function handleResponse<TResponse, TTransformedResponse>(
  apiResponse: ApiResponse<TResponse>,
  onTransform?: (result: TResponse) => TTransformedResponse
): ApiResponse<TTransformedResponse> {
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

  let res: ApiResponse<TTransformedResponse>;
  if (onTransform) {
    const transformed = onTransform(apiResponse.data ?? ({} as TResponse));

    res = {
      data: transformed,
      error: undefined,
      request: apiResponse.request,
      response: apiResponse.response
    } as ApiResponse<TTransformedResponse>;
  } else {
    res = {
      data: apiResponse.data,
      error: undefined,
      request: apiResponse.request,
      response: apiResponse.response
    } as ApiResponse<TTransformedResponse>;
  }

  return res;
}
