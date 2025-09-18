import { useQuery } from '@tanstack/react-query';
import { useCallback, useEffect, useRef, useState } from 'react';
import axios, { AxiosError, AxiosResponse } from 'axios';
import { recorder, SeverityLevel } from '../recorder.ts';
import { useOfflineService } from '../services/OfflineServiceContext.tsx';
import { ActionRequestData, ActionResult, modifyRequestData } from './Actions.ts';
import useApiErrorState from './ApiErrorState.ts';


export interface ActionQueryConfiguration<
  TRequestData extends ActionRequestData,
  ExpectedErrorCode extends string = '',
  TResponse = any,
  TTransformedResponse = any
> {
  // The generated AXIOS endpoint we need to call
  request: (
    requestData: TRequestData
  ) => Promise<
    | (AxiosResponse<TResponse, any> & { error: undefined })
    | (AxiosError<unknown, any> & { data: undefined; error: unknown })
  >;
  // Whether the request is tenanted or not
  isTenanted?: boolean;
  // The transformation function to apply to the response
  transform: (result: TResponse) => TTransformedResponse;
  // What kind of known errors are we expecting to handle ourselves
  passThroughErrors?: Record<number, ExpectedErrorCode>;
  // The cache key to use to store the response
  cacheKey: readonly unknown[];
}

// Use this hook for calling @hey-api generated AXIOS generated endpoints for GET or SEARCH.
// Use the useActionCommand hook for POST, PUT, PATCH or DELETE endpoints
// Supports automatic OrganizationId population for isTenanted requests
// Supports monitoring of requests for displaying progress indicators
// Supports monitoring of expected errors versus unexpected errors
// Supports monitoring of online/offline status
export default function useActionQuery<
  TRequestData extends ActionRequestData,
  TResponse = any,
  TTransformedResponse = any,
  ExpectedErrorCode extends string = any
>(
  configuration: ActionQueryConfiguration<TRequestData, ExpectedErrorCode, TResponse, TTransformedResponse>
): ActionResult<TRequestData, ExpectedErrorCode, TTransformedResponse> {
  const { request, passThroughErrors, cacheKey, transform: onSuccess } = configuration;

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
        try {
          const requestData = currentRequestDataRef.current ?? ({} as TRequestData);
          let res = await request(requestData);

          /* @hey-api/client-axios may return an AxiosError instead of throw the error
          See: https://github.com/hey-api/openapi-ts/blob/main/examples/openapi-ts-axios/src/client/client/client.gen.ts#L94-L106
           */
          if (res.status === undefined || res.status >= 400) {
            if (axios.isAxiosError(res)) {
              // noinspection ExceptionCaughtLocallyJS
              throw res as AxiosError<unknown, any> & { error: undefined };
            }
          }

          return await (res?.data ?? ({} as TResponse));
        } catch (error) {
          throw error;
        }
      } else {
        recorder.trace('useActionQuery: Cannot execute query when browser is offline', SeverityLevel.Warning);
        throw new Error('Cannot execute query action when browser is offline');
      }
    },
    select: onSuccess
  });

  useEffect(() => {
    if (!isError) {
      clearErrors();
    } else {
      handleError(queryError);
    }
  }, [isError, clearErrors]);

  const executeCallback = useCallback(
    (
      requestData?: TRequestData,
      {
        onSuccess
      }: {
        onSuccess?: (params: { requestData?: TRequestData; response: TTransformedResponse }) => void;
      } = {}
    ) => {
      let submittedRequestData: TRequestData = modifyRequestData(requestData, configuration.isTenanted);
      setCurrentRequestData(submittedRequestData);
      currentRequestDataRef.current = submittedRequestData;

      refetch({})
        .then((result) => onSuccess?.({ requestData, response: result.data as TTransformedResponse }))
        .catch((error) => {
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
    lastSuccessResponse: response,
    isSuccess: isCompleted,
    lastExpectedError: expectedError,
    lastUnexpectedError: unexpectedError,
    isExecuting,
    isReady: isOnline,
    lastRequestValues: isOnline ? variables : undefined
  };
}
