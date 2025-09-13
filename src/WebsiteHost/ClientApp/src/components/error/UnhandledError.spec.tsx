import { render, screen } from '@testing-library/react';
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
    stack: 'astacktrace',
    name: 'anerrorname',
    config: {} as any,
    isAxiosError: true,
    toJSON: () => ({})
  };

  it('renders nothing when no error is provided', () => {
    const { container } = render(<UnhandledError id="test" />);
    expect(container.firstChild).toBeNull();
  });

  it('renders error component with correct test ids', () => {
    render(<UnhandledError id="test" error={mockError} />);

    expect(screen.getByTestId('test_unhandled_error')).toBeDefined();
    expect(screen.getByTestId('test_unhandled_error_details')).toBeDefined();
  });

  it('displays error title and description', () => {
    render(<UnhandledError id="test" error={mockError} />);

    expect(screen.getByText('Unexpected Error')).toBeDefined();
    expect(screen.getByText('Sorry! This error was completely unexpected at this time!')).toBeDefined();
  });

  it('displays status code and status text', () => {
    render(<UnhandledError id="test" error={mockError} />);

    expect(screen.getByTestId('test_unhandled_error_details_statusCode').textContent).toBe('500');
    expect(screen.getByTestId('test_unhandled_error_details_statusText').textContent).toBe('amessage');
  });

  it('displays error code when present', () => {
    render(<UnhandledError id="test" error={mockError} />);

    expect(screen.getByTestId('test_unhandled_error_details_errorCode').textContent).toBe('anerrorcode');
  });

  it('hides error code when not present', () => {
    const errorWithoutCode = { ...mockError, code: undefined };
    render(<UnhandledError id="test" error={errorWithoutCode} />);

    expect(screen.queryByTestId('test_unhandled_error_details_errorCode')).toBeNull();
  });

  it('displays stack trace when present', () => {
    render(<UnhandledError id="test" error={mockError} />);

    const stackTraceElement = screen.getByTestId('test_unhandled_error_details_stackTrace');
    expect(stackTraceElement).toBeDefined();
    expect(stackTraceElement.textContent).toContain('astacktrace');
  });

  it('hides stack trace when not present', () => {
    const errorWithoutStack = { ...mockError, stack: undefined };
    render(<UnhandledError id="test" error={errorWithoutStack} />);

    expect(screen.queryByTestId('test_unhandled_error_details_stackTrace')).toBeNull();
  });

  it('handles error without response', () => {
    const networkError: AxiosError = {
      ...mockError,
      response: undefined,
      message: 'amessage',
      code: 'anerrorcode'
    };

    render(<UnhandledError id="test" error={networkError} />);

    expect(screen.getByTestId('test_unhandled_error_details_statusCode').textContent).toBe('');
    expect(screen.getByTestId('test_unhandled_error_details_statusText').textContent).toBe('amessage');
  });

  it('uses correct component id structure', () => {
    render(<UnhandledError id="custom-prefix" error={mockError} />);

    expect(screen.getByTestId('custom-prefix_unhandled_error')).toBeDefined();
    expect(screen.getByTestId('custom-prefix_unhandled_error_details')).toBeDefined();
    expect(screen.getByTestId('custom-prefix_unhandled_error_details_statusCode')).toBeDefined();
  });
});
