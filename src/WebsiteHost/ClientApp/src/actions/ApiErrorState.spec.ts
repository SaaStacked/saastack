import { act, renderHook } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { AxiosError } from 'axios';
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

  it('when handle unexpected regular error, then sets unexpected error', async () => {
    const { result } = renderHook(() => useApiErrorState(expectedErrorStatusCodes));

    await act(async () => result.current.onError(new Error('anerror') as any));

    expect(result.current.expectedError).toBeUndefined();
    expect(result.current.unexpectedError).toBeUndefined();
  });

  it('when handle unexpected Axios error, then sets unexpected error', async () => {
    vi.spyOn(recorder, 'crash');

    const { result } = renderHook(() => useApiErrorState(expectedErrorStatusCodes));

    await act(async () =>
      result.current.onError({
        isAxiosError: true,
        message: 'anerror',
        response: {
          status: 500,
          statusText: 'anerror',
          data: {
            title: 'atitle',
            details: 'adetails'
          },
          headers: {},
          config: {} as any
        }
      } as AxiosError as Error)
    );

    expect(result.current.expectedError).toBeUndefined();
    expect(result.current.unexpectedError).not.toBeUndefined();
    expect(result.current.unexpectedError?.isAxiosError).toBe(true);
    expect(result.current.unexpectedError?.message).toBe('anerror');
    expect(result.current.unexpectedError?.response).toStrictEqual({
      status: 500,
      statusText: 'anerror',
      data: {
        title: 'atitle',
        details: 'adetails'
      },
      headers: {},
      config: {} as any
    });
    expect(recorder.crash).toHaveBeenCalled();
  });

  it('when handle expected Axios error, then sets expected error', async () => {
    vi.spyOn(recorder, 'crash');

    const { result } = renderHook(() => useApiErrorState(expectedErrorStatusCodes));

    await act(async () =>
      result.current.onError({
        isAxiosError: true,
        message: 'anerror',
        response: {
          status: 400,
          statusText: 'anerror',
          data: {
            title: 'atitle',
            details: 'adetails'
          },
          headers: {},
          config: {} as any
        }
      } as AxiosError as Error)
    );

    expect(result.current.expectedError).not.toBeUndefined();
    expect(result.current.expectedError?.code).toBe('amessage');
    expect(result.current.expectedError?.response).toStrictEqual({
      title: 'atitle',
      details: 'adetails'
    });
    expect(result.current.unexpectedError).toBeUndefined();
    expect(recorder.crash).not.toHaveBeenCalled();
  });
});
