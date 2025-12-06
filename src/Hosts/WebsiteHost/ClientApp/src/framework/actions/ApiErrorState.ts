import { useCallback, useState } from 'react';
import { recorder } from '../recorder';
import { ErrorResponse } from './Actions.ts';

export interface ExpectedErrorDetails<TExpectedErrorCode extends string = ''> {
  code: TExpectedErrorCode;
  response?: any;
}

function hasKey<TObject extends Record<Key, any>, Key extends string | number | symbol>(
  obj: TObject,
  key: string | number | symbol
): key is keyof TObject {
  return key in obj;
}

const getExpectedError = <ExpectedErrorCode extends string = ''>(
  errorCodes: Record<number, ExpectedErrorCode>,
  error?: any,
  response?: Response
) => {
  if (error == undefined) {
    return undefined;
  }

  // Best guess at extracting the status code
  let statusCode = 0;
  if (response) {
    statusCode = response.status;
  } else if (error?.status) {
    statusCode = error.status;
  }

  if (!hasKey(errorCodes, statusCode)) {
    return undefined;
  }

  return {
    code: errorCodes[statusCode],
    response: error
  } as ExpectedErrorDetails<ExpectedErrorCode>;
};

function useApiErrorState<ExpectedErrorCode extends string = ''>(
  expectedErrorStatusCodes: Record<number, ExpectedErrorCode> = {}
) {
  const [expectedError, setExpectedError] = useState<ExpectedErrorDetails<ExpectedErrorCode> | undefined>();
  const [unexpectedError, setUnexpectedError] = useState<ErrorResponse | undefined>();

  const clearErrors = useCallback(() => {
    setExpectedError(undefined);
    setUnexpectedError(undefined);
  }, [setExpectedError, setUnexpectedError]);

  const onError = (error: any, response?: Response) => {
    const handledError = getExpectedError(expectedErrorStatusCodes, error, response);
    setExpectedError(handledError);

    if (handledError === undefined) {
      setUnexpectedError(
        response ? createErrorResponseWithResponse(error, response) : createErrorResponseWithSyntheticResponse(error)
      );
      if (error instanceof Error) {
        recorder.crash(error);
      } else {
        recorder.crash(new Error(`Unexpected error from API: ${error}`));
      }
    }
  };

  return {
    onError,
    clearErrors,
    expectedError,
    unexpectedError
  };
}

function createErrorResponseWithSyntheticResponse(error: unknown): ErrorResponse {
  return {
    data: error,
    response: { ok: false, status: 0, statusText: 'Internal Client Error' } as Response
  } as ErrorResponse;
}

function createErrorResponseWithResponse(error: unknown, response: Response): ErrorResponse {
  return {
    data: error,
    response
  } as ErrorResponse;
}

export default useApiErrorState;
