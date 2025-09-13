import { render, screen } from '@testing-library/react';
import React from 'react';
import { FormProvider, useForm } from 'react-hook-form';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ActionRequestData, ActionResult } from '../../../actions/Actions';
import { ActionContext } from '../Contexts';
import FormSubmitButton from './FormSubmitButton';

vi.mock('../../Button', () => ({
  default: ({ id, label, busy, disabled, type, ...props }: any) => (
    <button data-testid={id} disabled={disabled} type={type} {...props}>
      {busy ? 'Loading...' : label}
    </button>
  )
}));

interface TestRequestData extends ActionRequestData {
  atext: string;
  anemailaddress: string;
}

describe('FormSubmitButton', () => {
  let mockAction: ActionResult<TestRequestData, any, any>;
  const validationSchema = z.object({
    atext: z.string().min(1, 'amessage1'),
    anemailaddress: z.email('amessage2')
  });

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

  const FormWrapper = ({
    children,
    action = mockAction,
    defaultValues = { atext: 'aname', anemailaddress: 'auser@company.com' },
    isFormValid = true
  }: {
    children: React.ReactNode;
    action?: ActionResult<any, any>;
    defaultValues?: any;
    isFormValid?: boolean;
  }) => {
    const TestForm = () => {
      const methods = useForm({
        resolver: zodResolver(validationSchema),
        defaultValues,
        mode: 'onBlur'
      });

      // Simulate form validation state
      if (!isFormValid) {
        methods.setError('atext', { message: 'amessage' });
        // Trigger form submission to set isSubmitted
        methods.handleSubmit(() => {})();
      }

      return (
        <ActionContext.Provider value={action}>
          <FormProvider {...methods}>
            <form>{children}</form>
          </FormProvider>
        </ActionContext.Provider>
      );
    };

    return <TestForm />;
  };

  it('renders with default props', () => {
    render(
      <FormWrapper>
        <FormSubmitButton id="anid" />
      </FormWrapper>
    );

    const button = screen.getByTestId('anid_form_submit');
    expect(button).not.toBeNull();
    expect(button.textContent).toBe('Submit');
    expect(button.getAttribute('type')).toBe('submit');
  });

  it('renders with custom label', () => {
    render(
      <FormWrapper>
        <FormSubmitButton id="anid" label="alabel" />
      </FormWrapper>
    );

    const button = screen.getByTestId('anid_form_submit');
    expect(button.textContent).toBe('alabel');
  });

  it('when action is ready and form is valid, then enabled', () => {
    render(
      <FormWrapper action={{ ...mockAction, isReady: true }}>
        <FormSubmitButton id="anid" />
      </FormWrapper>
    );

    const button = screen.getByTestId('anid_form_submit') as HTMLButtonElement;
    expect(button.disabled).toBe(false);
  });

  it('when not ready, then disabled', () => {
    render(
      <FormWrapper action={{ ...mockAction, isReady: false }}>
        <FormSubmitButton id="anid" />
      </FormWrapper>
    );

    const button = screen.getByTestId('anid_form_submit') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
  });

  it('when action is executing, then shows busy state', () => {
    render(
      <FormWrapper action={{ ...mockAction, isExecuting: true }}>
        <FormSubmitButton id="anid" />
      </FormWrapper>
    );

    const button = screen.getByTestId('anid_form_submit');
    expect(button.textContent).toBe('Loading...');
  });

  it('handles missing action context gracefully', () => {
    const FormWithoutAction = ({ children }: { children: React.ReactNode }) => {
      const methods = useForm({
        defaultValues: { name: 'John', email: 'john@example.com' }
      });

      return (
        <FormProvider {...methods}>
          <form>{children}</form>
        </FormProvider>
      );
    };

    render(
      <FormWithoutAction>
        <FormSubmitButton id="anid" />
      </FormWithoutAction>
    );

    const button = screen.getByTestId('anid_form_submit') as HTMLButtonElement;
    expect(button).not.toBeNull();
    expect(button.disabled).toBe(true); // Should be disabled when no action
  });
});
