import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useOfflineService } from '../providers/OfflineServiceContext.tsx';
import { recorder, SeverityLevel } from '../recorder.ts';
import { ActionResult, ApiResponse, executeRequest, handleRequestError, modifyRequestData } from './Actions.ts';
import useApiErrorState from './ApiErrorState.ts';


export type InvalidateCacheKeysExpression<TRequestData = any> =
  | MutationCacheKeys
  | ((request: TRequestData) => MutationCacheKeys);
export type MutationCacheKeys = string[][] | ([] & { __brand: 'MutationCacheKeys' });
export type MutationCacheKeysAdvancedExpression<TRequestData = any, TResponse = any> = {
  key: InvalidateCacheKeysExpression<TRequestData>;
  toQueryData?: (response: ApiResponse<TResponse>, request: TRequestData, previous: unknown) => unknown;
};
export type MutationCacheKey<TRequestData = any, TResponse = any> =
  | InvalidateCacheKeysExpression<TRequestData>
  | MutationCacheKeysAdvancedExpression<TRequestData, TResponse>;
export type MutateCacheKeysExpression<TRequestData = any, TResponse = any> =
  | MutationCacheKey<TRequestData, TResponse>
  | Array<MutationCacheKey<TRequestData, TResponse>>
  | ((
      request: TRequestData
    ) => MutationCacheKey<TRequestData, TResponse> | Array<MutationCacheKey<TRequestData, TResponse>>);

function isCacheKeys(value: unknown): value is MutationCacheKeys {
  return (
    Array.isArray(value) &&
    value.every((element) => Array.isArray(element) && element.every((part) => typeof part === 'string'))
  );
}

function normalizeMutationKey<TRequestData, TResponse>(
  expression: MutationCacheKey<TRequestData, TResponse>
): MutationCacheKeysAdvancedExpression<TRequestData, TResponse> {
  return isCacheKeys(expression) || typeof expression === 'function'
    ? { key: expression as InvalidateCacheKeysExpression<TRequestData> }
    : (expression as MutationCacheKeysAdvancedExpression<TRequestData, TResponse>);
}

function toMutationKeys<TRequestData, TResponse>(
  mutateCacheKey:
    | MutationCacheKey<TRequestData, TResponse>
    | Array<MutationCacheKey<TRequestData, TResponse>>
    | undefined
): Array<MutationCacheKey<TRequestData, TResponse>> {
  if (!mutateCacheKey) {
    return [];
  }
  if (isCacheKeys(mutateCacheKey) || !Array.isArray(mutateCacheKey)) {
    return [mutateCacheKey as MutationCacheKey<TRequestData, TResponse>];
  }
  return mutateCacheKey;
}

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
  invalidateCacheKeys?: InvalidateCacheKeysExpression<TRequestData>;
  // The keys in the request cache that we want to mutate, in the case of successful response
  mutateCacheKey?: MutateCacheKeysExpression<TRequestData, TResponse>;
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
  const { request, passThroughErrors, onSuccess, invalidateCacheKeys, mutateCacheKey } = configuration;
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

      // Invalidate any cache keys first
      const cacheKeys =
        typeof invalidateCacheKeys === 'function' ? invalidateCacheKeys(requestData) : invalidateCacheKeys;
      if (cacheKeys && cacheKeys.length > 0) {
        for (const cacheKey of cacheKeys) {
          recorder.traceDebug('ActionCommand: clearing cache keyset: {Keys}', { cacheKey });
          queryClient.removeQueries({ queryKey: cacheKey, exact: false });
        }
      }

      // Mutate cache keys, but after invalidation so a freshly written value is not removed by a
      // broader `invalidateCacheKeys` entry that overlaps the seeded key.
      const resolvedUpdateCache = typeof mutateCacheKey === 'function' ? mutateCacheKey(requestData) : mutateCacheKey;
      const mutationExpressions = toMutationKeys(resolvedUpdateCache);
      for (const mutationExpression of mutationExpressions) {
        const normalizedKeys = normalizeMutationKey(mutationExpression);
        const mutationKeys =
          typeof normalizedKeys.key === 'function' ? normalizedKeys.key(requestData) : normalizedKeys.key;
        for (const mutationKey of mutationKeys) {
          recorder.traceDebug('ActionCommand: seeding cache keyset, from response: {Keys}', { seedKey: mutationKey });
          queryClient.setQueryData(mutationKey, (previous: unknown) =>
            normalizedKeys.toQueryData ? normalizedKeys.toQueryData(apiResponse, requestData, previous) : apiResponse
          );
        }
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

  const isExecuting = isPending && !isError && !isSuccess;
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
