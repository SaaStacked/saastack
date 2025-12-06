import { render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import '@testing-library/jest-dom';
import { ActionResult } from '../../actions/Actions';
import PageAction, { PageActionRef } from './PageAction.tsx';

interface TestRequestData {
  atext: string;
}

describe('PageAction', () => {
  let mockAction: ActionResult<TestRequestData, 'A_VALIDATION_ERROR', any>;

  beforeEach(() => {
    mockAction = {
      execute: vi.fn(),
      isSuccess: false,
      lastSuccessResponse: undefined,
      lastExpectedError: undefined,
      lastUnexpectedError: undefined,
      isExecuting: false,
      isReady: true,
      lastRequestValues: undefined
    };
  });

  it('renders with custom id', () => {
    render(<PageAction id="anid" action={mockAction as any} />);

    const hiddenDiv = screen.getByTestId('anid_page_action');
    expect(hiddenDiv).toBeInTheDocument();
  });

  it('when not executed, does not render children', () => {
    render(
      <PageAction id="anid" action={mockAction as any}>
        <div data-testid="achildid">Child content</div>
      </PageAction>
    );

    const childContent = screen.queryByTestId('achildid');
    expect(childContent).not.toBeInTheDocument();
  });

  it('when executing, does not render children', async () => {
    mockAction.isExecuting = true;

    render(
      <PageAction id="anid" action={mockAction as any}>
        <div data-testid="achildid">Child content</div>
      </PageAction>
    );

    const childContent = screen.queryByTestId('achildid');
    expect(childContent).not.toBeInTheDocument();
  });

  it('when execute succeeds, renders children', async () => {
    mockAction.isSuccess = true;
    mockAction.isExecuting = false;

    render(
      <PageAction id="anid" action={mockAction as any}>
        <div data-testid="achildid">Child content</div>
      </PageAction>
    );

    const childContent = screen.queryByTestId('achildid');
    expect(childContent).toBeInTheDocument();
  });

  it('when execute succeeds, calls onSuccess callback', async () => {
    const onSuccess = vi.fn();
    mockAction.execute = vi.fn((formData, options) =>
      options?.onSuccess?.({
        requestData: formData,
        response: { success: true }
      })
    );
    const ref = React.createRef<PageActionRef<any>>();

    render(<PageAction id="anid" action={mockAction as any} onSuccess={onSuccess} ref={ref} />);

    ref.current?.execute({ atext: 'aname' });

    await waitFor(() =>
      expect(onSuccess).toHaveBeenCalledWith({
        requestData: { atext: 'aname' },
        response: { success: true }
      })
    );
  });

  it('when execute fails with expected error, displays expected error', () => {
    mockAction.lastExpectedError = { code: 'A_VALIDATION_ERROR' as any };
    const expectedErrorMessages = { A_VALIDATION_ERROR: 'amessage' };

    render(<PageAction id="anid" action={mockAction as any} expectedErrorMessages={expectedErrorMessages} />);

    const expectedError = screen.getByTestId('anid_page_action_expected_error_alert');
    expect(expectedError).not.toBeNull();
    expect(expectedError.textContent).toBe('amessage');
  });

  it('when execute fails with unexpected error, displays unexpected error', () => {
    mockAction.lastUnexpectedError = new Error('anerror') as any;

    render(<PageAction id="anid" action={mockAction as any} />);

    const unexpectedError = screen.getByTestId('anid_page_action_unexpected_error_unhandled_error');
    expect(unexpectedError).not.toBeNull();
  });
});
