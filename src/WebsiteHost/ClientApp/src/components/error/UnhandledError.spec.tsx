import { act, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { AxiosError } from 'axios';
import UnhandledError from './UnhandledError';


describe('UnhandledError', () => {
  const mockError: AxiosError = {
    response: {
      status: 500,
      statusText: 'astatustext',
      headers: {},
      data: {},
      config: {} as any
    },
    message: 'amessage',
    code: 'anerrorcode',
    stack: 'aclientstacktrace',
    name: 'anerrorname',
    config: {} as any,
    isAxiosError: true,
    toJSON: () => ({})
  };

  it('when no error is provided, renders nothing', () => {
    const { container } = render(<UnhandledError id="anid" />);
    expect(container.firstChild).toBeNull();
  });

  it('displays error title and description', () => {
    render(<UnhandledError id="anid" error={mockError} />);

    expect(screen.getByText('components.unhandled_error.title')).toBeDefined();
    expect(screen.getByText('components.unhandled_error.message')).toBeDefined();
  });

  it('displays status code and error code', () => {
    render(<UnhandledError id="anid" error={mockError} />);

    expect(screen.getByTestId('anid_unhandled_error_details_statusCode').textContent).toBe('500');
    expect(screen.getByTestId('anid_unhandled_error_details_errorCode').textContent).toBe('astatustext');
  });

  it('displays error code', () => {
    render(<UnhandledError id="anid" error={mockError} />);

    expect(screen.getByTestId('anid_unhandled_error_details_errorCode').textContent).toBe('astatustext');
  });

  it('when no code, hides error code', () => {
    const errorWithoutCode = { ...mockError, response: undefined, code: undefined };
    render(<UnhandledError id="anid" error={errorWithoutCode} />);

    expect(screen.queryByTestId('anid_unhandled_error_details_errorCode')).toBeNull();
  });

  it('when stack traces, hidden behind collapsable details', () => {
    const errorWithStackTraces = {
      ...mockError,
      response: { ...mockError.response, data: { exception: 'astacktrace' } } as any
    };
    render(<UnhandledError id="anid" error={errorWithStackTraces} />);

    expect(screen.queryByTestId('anid_unhandled_error_details_clientStackTrace')).toBeNull();
    expect(screen.queryByTestId('anid_unhandled_error_details_serverStackTrace')).toBeNull();
  });

  it('when client stack trace and click details, displays client stack trace', async () => {
    render(<UnhandledError id="anid" error={mockError} />);

    await act(async () => {
      const button = screen.getByText('components.unhandled_error.technical_details');
      button.click();
    });

    const stackTraceElement = screen.getByTestId('anid_unhandled_error_details_clientStackTrace');
    expect(stackTraceElement).toBeDefined();
    expect(stackTraceElement.textContent).toContain('aclientstacktrace');
  });

  it('when server stack trace and click details, displays server stack trace', async () => {
    const errorWithStackTraces = {
      ...mockError,
      stack: undefined,
      response: { ...mockError.response, data: { exception: 'aserverstacktrace' } } as any
    };
    render(<UnhandledError id="anid" error={errorWithStackTraces} />);

    await act(async () => {
      const button = screen.getByText('components.unhandled_error.technical_details');
      button.click();
    });

    const serverStackTrace = screen.getByTestId('anid_unhandled_error_details_serverStackTrace');
    expect(serverStackTrace).toBeDefined();
    expect(serverStackTrace.textContent).toContain('aserverstacktrace');
  });

  it('handles error without response', () => {
    const networkError: AxiosError = {
      ...mockError,
      response: undefined,
      message: 'amessage',
      code: 'anerrorcode',
      status: 400
    };

    render(<UnhandledError id="anid" error={networkError} />);

    expect(screen.getByTestId('anid_unhandled_error_details_statusCode').textContent).toBe('400');
    expect(screen.getByTestId('anid_unhandled_error_details_errorCode').textContent).toBe('anerrorcode');
    expect(screen.getByTestId('anid_unhandled_error_details_errorMessage').textContent).toBe('amessage');
  });

  it('handles error with response', () => {
    const networkError: AxiosError = {
      ...mockError,
      response: { status: 400, statusText: 'astatustext', data: { detail: 'adetail' } } as any,
      message: 'amessage',
      code: 'anerrorcode',
      status: 999
    };

    render(<UnhandledError id="anid" error={networkError} />);

    expect(screen.getByTestId('anid_unhandled_error_details_statusCode').textContent).toBe('400');
    expect(screen.getByTestId('anid_unhandled_error_details_errorCode').textContent).toBe('astatustext');
    expect(screen.getByTestId('anid_unhandled_error_details_errorMessage').textContent).toBe('adetail');
  });
});
