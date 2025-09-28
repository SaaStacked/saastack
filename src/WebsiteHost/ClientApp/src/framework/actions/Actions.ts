import { AxiosError } from 'axios';
import { useCurrentUser } from '../providers/CurrentUserContext.tsx';
import { ExpectedErrorDetails } from './ApiErrorState.ts';


export interface ActionResult<TRequestData = any, ExpectedErrorCode extends string = '', TResponse = any> {
  // To execute the XHR request
  execute: (
    requestData?: TRequestData,
    options?: { onSuccess?: (params: { requestData?: TRequestData; response: TResponse }) => void }
  ) => void;
  // Whether the action completed a successful XHR request
  isSuccess?: boolean;
  // The last returned successful XHR response
  lastSuccessResponse?: TResponse;
  // The last returned expected XHR error
  lastExpectedError?: ExpectedErrorDetails<ExpectedErrorCode>;
  // The last returned unexpected error
  lastUnexpectedError?: AxiosError;
  // Whether the action is already executing
  isExecuting: boolean;
  // Whether the browser is online and able to execute
  isReady: boolean;
  // The actual values sent in the last request
  lastRequestValues?: TRequestData | undefined;
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
