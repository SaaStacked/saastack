import { act, renderHook } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { ProblemDetails } from '../api/apiHost1';
import { recorder } from '../recorder';
import useApiErrorState from './ApiErrorState';

describe('useApiErrorState', () => {
  const expectedErrorStatusCodes = {
    400: 'amessage'
  } as Record<number, string>;

  it('when clear before handle, then clears errors', async () => {
    const { result } = renderHook(() => useApiErrorState(expectedErrorStatusCodes));

    await act(async () => result.current.clearErrors());

    expect(result.current.expectedError).toBeUndefined();
    expect(result.current.unexpectedError).toBeUndefined();
  });

  it('when clear after handle, then clears errors', async () => {
    const { result } = renderHook(() => useApiErrorState(expectedErrorStatusCodes));

    await act(async () => {
      result.current.onError(new Error('anerror') as any);
      result.current.clearErrors();
    });

    expect(result.current.expectedError).toBeUndefined();
    expect(result.current.unexpectedError).toBeUndefined();
  });

  it('when handle JavaScript error with no response, then sets unexpected error', async () => {
    const { result } = renderHook(() => useApiErrorState(expectedErrorStatusCodes));

    const javaScriptError = new Error('anerror');
    await act(async () => result.current.onError(javaScriptError));

    expect(result.current.expectedError).toBeUndefined();
    expect(result.current.unexpectedError).toBeDefined();
    expect(result.current.unexpectedError.data).toBe(javaScriptError);
    expect(result.current.unexpectedError.response.status).toBe(0);
    expect(result.current.unexpectedError.response.statusText).toBe('Internal Client Error');
  });

  it('when handle random data with no response, then sets unexpected error with synthetic response', async () => {
    vi.spyOn(recorder, 'crash');

    const { result } = renderHook(() => useApiErrorState(expectedErrorStatusCodes));

    const randomData = {
      title: 'atitle',
      details: 'adetails'
    };
    await act(async () => result.current.onError(randomData));

    expect(result.current.expectedError).toBeUndefined();
    expect(result.current.unexpectedError).toBeDefined();
    expect(result.current.unexpectedError?.data).toStrictEqual(randomData);
    expect(result.current.unexpectedError?.response).toStrictEqual({
      ok: false,
      status: 0,
      statusText: 'Internal Client Error'
    });
    expect(recorder.crash).toHaveBeenCalled();
  });

  it('when handle problem details with no response, then sets expected error', async () => {
    vi.spyOn(recorder, 'crash');

    const { result } = renderHook(() => useApiErrorState(expectedErrorStatusCodes));

    const problem = {
      type: 'atype',
      title: 'atitle',
      status: 400,
      details: 'adetails',
      instance: 'aninstance'
    } as unknown as ProblemDetails;
    await act(async () => result.current.onError(problem));

    expect(result.current.expectedError).toBeDefined();
    expect(result.current.expectedError?.code).toBe('amessage');
    expect(result.current.expectedError?.response).toStrictEqual(problem);
    expect(result.current.unexpectedError).toBeUndefined();
    expect(recorder.crash).not.toHaveBeenCalled();
  });

  it('when handle random data with response, then sets expected error', async () => {
    vi.spyOn(recorder, 'crash');

    const { result } = renderHook(() => useApiErrorState(expectedErrorStatusCodes));

    const randomData = {
      title: 'atitle',
      details: 'adetails'
    };
    await act(async () => result.current.onError(randomData, { ok: false, status: 400 } as Response));

    expect(result.current.expectedError).toBeDefined();
    expect(result.current.expectedError?.code).toBe('amessage');
    expect(result.current.expectedError?.response).toStrictEqual(randomData);
    expect(result.current.unexpectedError).toBeUndefined();
    expect(recorder.crash).not.toHaveBeenCalled();
  });

  it('when handle random data with unexpected error response, then sets unexpected error', async () => {
    vi.spyOn(recorder, 'crash');

    const { result } = renderHook(() => useApiErrorState(expectedErrorStatusCodes));

    const randomData = {
      title: 'atitle',
      details: 'adetails'
    };
    await act(async () =>
      result.current.onError(randomData, { ok: false, status: 500, statusText: 'Internal Server Error' } as Response)
    );

    expect(result.current.expectedError).toBeUndefined();
    expect(result.current.unexpectedError).toBeDefined();
    expect(result.current.unexpectedError?.data).toStrictEqual(randomData);
    expect(result.current.unexpectedError?.response.status).toBe(500);
    expect(result.current.unexpectedError?.response.statusText).toBe('Internal Server Error');
    expect(recorder.crash).toHaveBeenCalled();
  });
});
