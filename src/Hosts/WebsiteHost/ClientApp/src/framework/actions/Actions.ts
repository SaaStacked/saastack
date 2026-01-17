import { useCurrentUser } from '../providers/CurrentUserContext.tsx';
import { recorder } from '../recorder.ts';
import { ExpectedErrorDetails } from './ApiErrorState.ts';

export interface ErrorResponse {
  data: unknown | undefined;
  response: Response;
}

export type ApiResponse<TResponse> =
  | ({ data: TResponse; error: undefined } & { request: Request; response: Response })
  | ({ data: undefined; error: unknown } & { request: Request; response: Response });

export interface ActionResult<TRequestData = any, ExpectedErrorCode extends string = '', TResponse = any> {
  // To execute the XHR request
  execute: (
    requestData?: TRequestData,
    options?: {
      onSuccess?: (params: {
        requestData?: TRequestData;
        response: TResponse;
        statusCode: number;
        headers: Headers;
      }) => void;
    }
  ) => void;
  // Whether the action completed a successful XHR request
  isSuccess?: boolean;
  // The last returned successful XHR response
  lastSuccessResponse?: TResponse;
  // The last returned expected XHR error
  lastExpectedError?: ExpectedErrorDetails<ExpectedErrorCode>;
  // The last returned unexpected error
  lastUnexpectedError?: ErrorResponse;
  // Whether the action is already executing
  isExecuting: boolean;
  // Whether the browser is online and able to execute
  isReady: boolean;
  // The actual values sent in the last request
  lastRequestValues?: TRequestData | undefined;
}

export function isErrorResponse(error: any): error is ErrorResponse {
  return (
    error &&
    'data' in error &&
    'response' in error &&
    // dont match ApiResponse
    !('error' in error) &&
    !('request' in error)
  );
}

export function isApiResponse(error: any): error is ApiResponse<unknown> {
  return error && 'data' in error && 'error' in error && 'response' in error && 'request' in error;
}

export async function executeRequest<TRequestData, TResponse>(
  request: (requestData: TRequestData) => Promise<ApiResponse<TResponse>>,
  requestData: TRequestData
) {
  try {
    let res = await request(requestData);
    if (res.response && res.response.ok) {
      return {
        data: res.data ?? ({} as TResponse),
        request: res.request,
        response: res.response
      } as ApiResponse<TResponse>;
    } else {
      recorder.traceDebug('Action: Returned error: {Error}', { error: res.error });
      return Promise.reject({
        data: res.error,
        response: res.response
      } as ErrorResponse);
    }
  } catch (error) {
    throw error;
  }
}

export function handleRequestError(error: Error, handleError: (error: any, response?: Response) => void) {
  if (isErrorResponse(error)) {
    recorder.traceDebug('ActionCommand: Command returned error: {Error}', { error: error.data });
    handleError(error.data, error.response);
  } else {
    if (isApiResponse(error)) {
      recorder.traceDebug('ActionCommand: Command returned error: {Error}', { error: error.error });
      handleError(error.error, error.response);
    } else {
      recorder.traceDebug('ActionCommand: Command returned error: {Error}', { error });
      handleError(error);
    }
  }
}

export function modifyRequestData<TRequestData = any>(requestData?: TRequestData, isTenanted?: boolean): TRequestData {
  let submittedRequestData: TRequestData = requestData ?? ({} as TRequestData);
  const hasOrganizationId = () =>
    submittedRequestData && typeof submittedRequestData === 'object' && 'organizationId' in submittedRequestData;

  if (!hasOrganizationId()) {
    if (isTenanted) {
      const currentUser = useCurrentUser();
      const defaultOrganizationId = currentUser?.profile.defaultOrganizationId;
      if (defaultOrganizationId) {
        submittedRequestData = { ...submittedRequestData, organizationId: defaultOrganizationId } as TRequestData;
      }
    }
  }

  return submittedRequestData;
}
