import { act, fireEvent, screen } from '@testing-library/react';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import '@testing-library/jest-dom';
import { ActionResult } from '../../actions/Actions';
import { renderWithTestingProviders } from '../../testing/TestingProviders';
import { BusyLabelRevertAfterMs } from '../form/formSubmitButton/FormSubmitButton';
import ButtonAction from './ButtonAction';

interface TestRequestData {
  atext: string;
}

describe('ButtonAction', () => {
  let mockAction: ActionResult<TestRequestData, 'A_VALIDATION_ERROR', any>;

  beforeEach(() => {
    vi.useFakeTimers();
    mockAction = {
      execute: vi.fn(),
      isSuccess: undefined,
      lastSuccessResponse: undefined,
      lastExpectedError: undefined,
      lastUnexpectedError: undefined,
      isExecuting: false,
      isReady: true,
      lastRequestValues: undefined
    };
  });

  afterEach(() => vi.useRealTimers());

  it('renders with default props', () => {
    renderWithTestingProviders(<ButtonAction id="anid" action={mockAction} />);

    const button = screen.getByTestId('anid_button_action_button');
    expect(button).toBeInTheDocument();
    expect(button).toHaveTextContent('components.button.button_action.default_label');
    expect(button).not.toBeDisabled();
  });

  it('when execute succeeds, calls onSuccess callback', async () => {
    const onSuccess = vi.fn();
    mockAction.execute = vi.fn((formData, options) =>
      options?.onSuccess?.({
        requestData: formData,
        response: { success: true }
      })
    );

    renderWithTestingProviders(
      <ButtonAction id="anid" action={mockAction} onSuccess={onSuccess} requestData={{ atext: 'aname' }} />
    );

    const button = screen.getByTestId('anid_button_action_button');
    fireEvent.click(button);

    expect(onSuccess).toHaveBeenCalledWith({
      requestData: { atext: 'aname' },
      response: { success: true }
    });
  });

  it('when execute fails with expected error, displays expected error', () => {
    mockAction.lastExpectedError = { code: 'A_VALIDATION_ERROR' as any };
    const expectedErrorMessages = { A_VALIDATION_ERROR: 'amessage' };

    renderWithTestingProviders(
      <ButtonAction id="anid" action={mockAction} expectedErrorMessages={expectedErrorMessages} />
    );

    const button = screen.getByTestId('anid_button_action_button');
    fireEvent.click(button);

    const expectedError = screen.getByTestId('anid_button_action_expected_error_alert');
    expect(expectedError).not.toBeNull();
    expect(expectedError.textContent).toBe('amessage');
  });

  it('when execute fails with unexpected error, displays unexpected error', () => {
    mockAction.lastUnexpectedError = new Error('anerror') as any;

    renderWithTestingProviders(<ButtonAction id="anid" action={mockAction} />);

    const button = screen.getByTestId('anid_button_action_button');
    fireEvent.click(button);

    const unexpectedError = screen.getByTestId('anid_button_action_unexpected_error_unhandled_error');
    expect(unexpectedError).not.toBeNull();
  });

  it('when no label, renders with default label', async () => {
    renderWithTestingProviders(<ButtonAction id="anid" action={mockAction} />);

    const button = screen.getByTestId('anid_button_action_button');
    expect(button.textContent).toBe('components.button.button_action.default_label');
  });

  it('when no busy Label, and busy, renders with default busy label', async () => {
    mockAction.isExecuting = true;

    renderWithTestingProviders(<ButtonAction id="anid" action={mockAction} />);

    const button = screen.getByTestId('anid_button_action_button');
    expect(button.textContent).toBe('components.button.button_action.default_busy_label');
  });

  it('when busy, renders with busy label', async () => {
    mockAction.isExecuting = true;

    renderWithTestingProviders(<ButtonAction id="anid" action={mockAction} busyLabel="abusylabel" />);

    const button = screen.getByTestId('anid_button_action_button');
    expect(button.textContent).toBe('abusylabel');
  });

  it('when no complete Label, and completed, renders with default complete label', async () => {
    mockAction.isSuccess = true;

    renderWithTestingProviders(<ButtonAction id="anid" action={mockAction} busyLabel="abusylabel" />);

    const button = screen.getByTestId('anid_button_action_button');
    expect(button.textContent).toBe('components.button.button_action.default_completed_label');

    await act(() => vi.advanceTimersByTime(BusyLabelRevertAfterMs));

    expect(button.textContent).toBe('components.button.button_action.default_label');
  });

  it('when complete, renders with complete label', async () => {
    mockAction.isSuccess = true;

    renderWithTestingProviders(<ButtonAction id="anid" action={mockAction} completeLabel="acompletelabel" />);

    const button = screen.getByTestId('anid_button_action_button');
    expect(button.textContent).toBe('acompletelabel');

    await act(() => vi.advanceTimersByTime(BusyLabelRevertAfterMs));

    expect(button.textContent).toBe('components.button.button_action.default_label');
  });

  it('when action is ready, then enabled', async () => {
    mockAction.isReady = true;

    renderWithTestingProviders(<ButtonAction id="anid" action={mockAction} />);

    const button = screen.getByTestId('anid_button_action_button') as HTMLButtonElement;
    expect(button.disabled).toBe(false);
  });

  it('when not ready, then disabled', async () => {
    mockAction.isReady = false;

    renderWithTestingProviders(<ButtonAction id="anid" action={mockAction} />);

    const button = screen.getByTestId('anid_button_action_button') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
  });
});
