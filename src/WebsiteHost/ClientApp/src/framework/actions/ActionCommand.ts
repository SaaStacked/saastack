import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useCallback, useEffect } from 'react';
import axios, { AxiosError, AxiosResponse } from 'axios';
import { useOfflineService } from '../providers/OfflineServiceContext.tsx';
import { recorder, SeverityLevel } from '../recorder.ts';
import { ActionResult, modifyRequestData } from './Actions.ts';
import useApiErrorState from './ApiErrorState.ts';


export interface ActionCommandConfiguration<TRequestData = any, ExpectedErrorCode extends string = '', TResponse = any> {
  // The generated AXIOS endpoint we need to call
  request: (
    requestData: TRequestData,
    throwOnError?: boolean
  ) => Promise<
    | (AxiosResponse<TResponse, any> & { error: undefined })
    | (AxiosError<unknown, any> & { data: undefined; error: unknown })
  >;
  // Whether the request is tenanted or not
  isTenanted?: boolean;
  // What to do in the case of a successful response
  onSuccess?: (response: TResponse) => void;
  // What kind of known errors are we expecting to handle ourselves
  passThroughErrors?: Record<number, ExpectedErrorCode>;
  // The keys in the request cache that we want to invalidate, in the case of successful response
  invalidateCacheKeys?: readonly unknown[];
}

// Use this hook for calling @hey-api generated AXIOS generated endpoints for POST, PUT, PATCH or DELETE.
// Use the useActionQuery hook for GET or SEARCH endpoints
// Supports automatic OrganizationId population for isTenanted requests
// Supports monitoring of requests for displaying progress indicators
// Supports monitoring of expected errors versus unexpected errors
// Supports monitoring of online/offline status
export function useActionCommand<TRequestData = any, TResponse = any, ExpectedErrorCode extends string = any>(
  configuration: ActionCommandConfiguration<TRequestData, ExpectedErrorCode, TResponse>
): ActionResult<TRequestData, ExpectedErrorCode, TResponse> {
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
    error: mutationError,
    variables
  } = useMutation({
    mutationFn: async (requestData) => {
      isOnline = offlineService && offlineService.status === 'online';
      if (isOnline) {
        try {
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

          return await (res.data ?? ({} as TResponse));
        } catch (error) {
          throw error;
        }
      } else {
        recorder.trace('useActionCommand: Cannot execute command when browser is offline', SeverityLevel.Warning);
        throw new Error('Cannot execute command action when browser is offline');
      }
    },
    onSuccess: (result: TResponse, _: TRequestData) => {
      if (onSuccess) {
        onSuccess(result);
      }

      if (invalidateCacheKeys) {
        queryClient.invalidateQueries({
          queryKey: invalidateCacheKeys,
          exact: false // we want to support wildcards
        });
      }
    }
  });

  useEffect(() => {
    if (!isError) {
      clearErrors();
    } else {
      handleError(mutationError);
    }
  }, [isError, clearErrors]);

  const executeCallback = useCallback(
    (
      requestData?: TRequestData,
      { onSuccess }: { onSuccess?: (params: { requestData?: TRequestData; response: TResponse }) => void } = {}
    ) => {
      let submittedRequestData: TRequestData = modifyRequestData(requestData, configuration.isTenanted);

      mutate(submittedRequestData, {
        onSuccess: (response, requestData) => onSuccess?.({ requestData, response })
      });
    },
    [mutate]
  );

  const isExecuting = isPending && isError == false && isSuccess == false;
  const isCompleted = isPending ? undefined : isSuccess ? true : isError ? false : undefined;
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
