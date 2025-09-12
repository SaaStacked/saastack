import { useCallback, useState } from 'react';
import axios, { AxiosError } from 'axios';
import { recorder } from '../recorder';


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
  error?: AxiosError
) => {
  if (error == undefined) {
    return undefined;
  }

  const statusCode = error?.response?.status;
  if (!statusCode) {
    return undefined;
  }

  if (!hasKey(errorCodes, statusCode)) {
    return undefined;
  }

  return {
    code: errorCodes[statusCode],
    response: error?.response?.data
  } as ExpectedErrorDetails<ExpectedErrorCode>;
};

function useApiErrorState<ExpectedErrorCode extends string = ''>(
  expectedErrorStatusCodes: Record<number, ExpectedErrorCode> = {}
) {
  const [expectedError, setExpectedError] = useState<ExpectedErrorDetails<ExpectedErrorCode> | undefined>();
  const [unexpectedError, setUnexpectedError] = useState<AxiosError | undefined>();

  const clearErrors = useCallback(() => {
    setExpectedError(undefined);
    setUnexpectedError(undefined);
  }, [setExpectedError, setUnexpectedError]);

  const onError = (error: Error) => {
    if (axios.isAxiosError(error)) {
      const handledError = getExpectedError(expectedErrorStatusCodes, error);
      setExpectedError(handledError);

      if (handledError === undefined) {
        setUnexpectedError(error);
        recorder.crash(error);
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

export default useApiErrorState;
