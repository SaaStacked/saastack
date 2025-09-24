import { render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import '@testing-library/jest-dom';
import { ActionRequestData, ActionResult } from '../../actions/Actions';
import HiddenAction, { HiddenActionRef } from './HiddenAction';

interface TestRequestData extends ActionRequestData {
  atext: string;
}

describe('HiddenAction', () => {
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

  it('renders with default props', () => {
    render(<HiddenAction id="anid" action={mockAction as any} />);

    const hiddenDiv = screen.getByTestId('anid_hidden_action');
    expect(hiddenDiv).toBeInTheDocument();
    expect(hiddenDiv).toHaveClass('hidden');
  });

  it('renders with custom id', () => {
    render(<HiddenAction id="anid" action={mockAction as any} />);

    const hiddenDiv = screen.getByTestId('anid_hidden_action');
    expect(hiddenDiv).toBeInTheDocument();
  });

  it('renders children inside hidden div', () => {
    render(
      <HiddenAction id="anid" action={mockAction as any}>
        <div data-testid="child-content">Child content</div>
      </HiddenAction>
    );

    const childContent = screen.getByTestId('child-content');
    expect(childContent).toBeInTheDocument();
    expect(childContent.textContent).toBe('Child content');
  });

  it('when execute succeeds, calls onSuccess callback', async () => {
    const onSuccess = vi.fn();
    mockAction.execute = vi.fn((formData, options) =>
      options?.onSuccess?.({
        requestData: formData,
        response: { success: true }
      })
    );
    const ref = React.createRef<HiddenActionRef<any>>();

    render(<HiddenAction id="anid" action={mockAction as any} onSuccess={onSuccess} ref={ref} />);

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

    render(<HiddenAction id="anid" action={mockAction as any} expectedErrorMessages={expectedErrorMessages} />);

    const expectedError = screen.getByTestId('anid_hidden_action_expected_error_alert');
    expect(expectedError).not.toBeNull();
    expect(expectedError.textContent).toBe('amessage');
  });

  it('when execute fails with unexpected error, displays unexpected error', () => {
    mockAction.lastUnexpectedError = new Error('anerror') as any;

    render(<HiddenAction id="anid" action={mockAction as any} />);

    const unexpectedError = screen.getByTestId('anid_hidden_action_unexpected_error_unhandled_error');
    expect(unexpectedError).not.toBeNull();
  });
});
