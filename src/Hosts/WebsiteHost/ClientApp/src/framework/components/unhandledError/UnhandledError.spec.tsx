import { act, render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { ErrorResponse } from '../../actions/Actions.ts';
import { ProblemDetails } from '../../api/apiHost1';
import UnhandledError from './UnhandledError';

describe('UnhandledError', () => {
  const unexpectedError: ErrorResponse = {
    data: {
      title: 'atitle',
      details: 'adetails',
      errors: [{ rule: 'arule', reason: 'areason', value: 'avalue' }],
      exception: 'aserverstacktrace',
      status: 500,
      type: 'atype',
      instance: 'aninstance',
      extensions: { akey: 'avalue' }
    },
    response: { status: 500, statusText: 'astatustext' } as Response
  };

  it('when no error is provided, renders nothing', () => {
    const { container } = render(<UnhandledError id="anid" />);
    expect(container.firstChild).toBeNull();
  });

  it('when error provided, then displays error title, subtitle and details link', () => {
    render(<UnhandledError id="anid" error={unexpectedError} />);

    expect(screen.getByText('components.unhandled_error.title')).toBeDefined();
    expect(screen.getByText('components.unhandled_error.subtitle')).toBeDefined();
    expect(screen.getByText('components.unhandled_error.links.details')).toBeDefined();
    expect(screen.queryByText('components.unhandled_error.status')).toBeNull();
  });

  it('when clicked details, displays status code and error code', async () => {
    render(<UnhandledError id="anid" error={unexpectedError} />);

    await act(async () => {
      const details = screen.getByText('components.unhandled_error.links.details');
      details.click();
    });

    expect(screen.getByTestId('anid_unhandled_error_details_statusCode').textContent).toBe('500');
    expect(screen.getByTestId('anid_unhandled_error_details_errorCode').textContent).toBe('astatustext');
  });

  it('when clicked details, displays error code', async () => {
    render(<UnhandledError id="anid" error={unexpectedError} />);

    await act(async () => {
      const details = screen.getByText('components.unhandled_error.links.details');
      details.click();
    });

    expect(screen.getByTestId('anid_unhandled_error_details_errorCode').textContent).toBe('astatustext');
  });

  it('when no code, hides error code', () => {
    const errorWithoutCode = { ...unexpectedError, code: undefined };
    render(<UnhandledError id="anid" error={errorWithoutCode} />);

    expect(screen.queryByTestId('anid_unhandled_error_details_errorCode')).toBeNull();
  });

  it('when stack traces, hidden behind collapsable details', () => {
    const errorWithStackTraces = {
      ...unexpectedError,
      response: { ...unexpectedError.response, data: { exception: 'astacktrace' } } as any
    };
    render(<UnhandledError id="anid" error={errorWithStackTraces} />);

    expect(screen.queryByTestId('anid_unhandled_error_details_clientStackTrace')).toBeNull();
    expect(screen.queryByTestId('anid_unhandled_error_details_serverStackTrace')).toBeNull();
  });

  it('when client stack trace and click details, displays client stack trace', async () => {
    const errorWithStackTraces = {
      data: new Error('aclientstacktrace'),
      response: { status: 500, statusText: 'astatustext' } as any
    };
    render(<UnhandledError id="anid" error={errorWithStackTraces} />);

    await act(async () => {
      const details = screen.getByText('components.unhandled_error.links.details');
      details.click();
    });
    await act(async () => {
      const moreDetails = screen.getByText('components.unhandled_error.links.more_details');
      moreDetails.click();
    });

    const stackTraceElement = screen.getByTestId('anid_unhandled_error_details_clientStackTrace');
    expect(stackTraceElement).toBeDefined();
    expect(stackTraceElement.textContent).toContain('aclientstacktrace');
  });

  it('when server stack trace and click details, displays server stack trace', async () => {
    const errorWithStackTraces = {
      ...unexpectedError,
      stack: undefined,
      response: { ...unexpectedError.response, data: { exception: 'aserverstacktrace' } } as any
    };
    render(<UnhandledError id="anid" error={errorWithStackTraces} />);

    await act(async () => {
      const details = screen.getByText('components.unhandled_error.links.details');
      details.click();
    });
    await act(async () => {
      const moreDetails = screen.getByText('components.unhandled_error.links.more_details');
      moreDetails.click();
    });

    const serverStackTrace = screen.getByTestId('anid_unhandled_error_details_serverStackTrace');
    expect(serverStackTrace).toBeDefined();
    expect(serverStackTrace.textContent).toContain('aserverstacktrace');
  });

  it('handles empty error without response', async () => {
    const error: ErrorResponse = {
      data: undefined,
      response: undefined as any
    };

    render(<UnhandledError id="anid" error={error} />);

    await act(async () => {
      const details = screen.getByText('components.unhandled_error.links.details');
      details.click();
    });

    expect(screen.getByTestId('anid_unhandled_error_details_statusCode').textContent).toBe('unknown');
    expect(screen.queryByTestId('anid_unhandled_error_details_errorCode')).toBeNull();
    expect(screen.getByTestId('anid_unhandled_error_details_errorMessage').textContent).toBe('unknown');
  });

  it('handles empty error with response', async () => {
    const error: ErrorResponse = {
      data: undefined,
      response: { status: 400, statusText: 'astatustext', data: { detail: 'adetail' } } as any
    };

    render(<UnhandledError id="anid" error={error} />);

    await act(async () => {
      const details = screen.getByText('components.unhandled_error.links.details');
      details.click();
    });

    expect(screen.getByTestId('anid_unhandled_error_details_statusCode').textContent).toBe('400');
    expect(screen.getByTestId('anid_unhandled_error_details_errorCode').textContent).toBe('astatustext');
    expect(screen.getByTestId('anid_unhandled_error_details_errorMessage').textContent).toBe('unknown');
  });

  it('handles Javascript error without response', async () => {
    const networkError: ErrorResponse = {
      data: new Error('anerror'),
      response: undefined as any
    };

    render(<UnhandledError id="anid" error={networkError} />);

    await act(async () => {
      const details = screen.getByText('components.unhandled_error.links.details');
      details.click();
    });

    expect(screen.getByTestId('anid_unhandled_error_details_statusCode').textContent).toBe('unknown');
    expect(screen.queryByTestId('anid_unhandled_error_details_errorCode')).toBeNull();
    expect(screen.getByTestId('anid_unhandled_error_details_errorMessage').textContent).toBe('anerror');
  });

  it('handles Javascript error with response', async () => {
    const networkError: ErrorResponse = {
      data: new Error('anerror'),
      response: { status: 400, statusText: 'astatustext', data: { detail: 'adetail' } } as any
    };

    render(<UnhandledError id="anid" error={networkError} />);

    await act(async () => {
      const details = screen.getByText('components.unhandled_error.links.details');
      details.click();
    });

    expect(screen.getByTestId('anid_unhandled_error_details_statusCode').textContent).toBe('400');
    expect(screen.queryByTestId('anid_unhandled_error_details_errorCode')).toBeNull();
    expect(screen.getByTestId('anid_unhandled_error_details_errorMessage').textContent).toBe('anerror');
  });

  it('handles ProblemDetails error without response', async () => {
    const networkError: ErrorResponse = {
      data: {
        type: 'atype',
        title: 'atitle',
        status: 400,
        detail: 'adetail',
        instance: 'aninstance'
      } as ProblemDetails,
      response: undefined as any
    };

    render(<UnhandledError id="anid" error={networkError} />);

    await act(async () => {
      const details = screen.getByText('components.unhandled_error.links.details');
      details.click();
    });

    expect(screen.getByTestId('anid_unhandled_error_details_statusCode').textContent).toBe('400');
    expect(screen.getByTestId('anid_unhandled_error_details_errorCode').textContent).toBe('atitle');
    expect(screen.getByTestId('anid_unhandled_error_details_errorMessage').textContent).toBe('adetail');
  });

  it('handles ProblemDetails error with response', async () => {
    const networkError: ErrorResponse = {
      data: {
        type: 'atype',
        title: 'atitle',
        status: 400,
        detail: 'adetail',
        instance: 'aninstance'
      } as ProblemDetails,
      response: { status: 499, statusText: 'astatustext', data: { detail: 'adetail' } } as any
    };

    render(<UnhandledError id="anid" error={networkError} />);

    await act(async () => {
      const details = screen.getByText('components.unhandled_error.links.details');
      details.click();
    });

    expect(screen.getByTestId('anid_unhandled_error_details_statusCode').textContent).toBe('499');
    expect(screen.getByTestId('anid_unhandled_error_details_errorCode').textContent).toBe('astatustext');
    expect(screen.getByTestId('anid_unhandled_error_details_errorMessage').textContent).toBe('adetail');
  });

  it('handles random error without response', async () => {
    const networkError: ErrorResponse = {
      data: {
        message: 'amessage',
        status: 400
      } as any,
      response: undefined as any
    };

    render(<UnhandledError id="anid" error={networkError} />);

    await act(async () => {
      const details = screen.getByText('components.unhandled_error.links.details');
      details.click();
    });

    expect(screen.getByTestId('anid_unhandled_error_details_statusCode').textContent).toBe('400');
    expect(screen.queryByTestId('anid_unhandled_error_details_errorCode')).toBeNull();
    expect(screen.getByTestId('anid_unhandled_error_details_errorMessage').textContent).toBe('amessage');
  });

  it('handles random error with response', async () => {
    const networkError: ErrorResponse = {
      data: {
        message: 'amessage',
        status: 400
      } as any,
      response: { status: 499, statusText: 'astatustext', data: { detail: 'adetail' } } as any
    };

    render(<UnhandledError id="anid" error={networkError} />);

    await act(async () => {
      const details = screen.getByText('components.unhandled_error.links.details');
      details.click();
    });

    expect(screen.getByTestId('anid_unhandled_error_details_statusCode').textContent).toBe('499');
    expect(screen.getByTestId('anid_unhandled_error_details_errorCode').textContent).toBe('astatustext');
    expect(screen.getByTestId('anid_unhandled_error_details_errorMessage').textContent).toBe('amessage');
  });
});
