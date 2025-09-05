import { act, render, screen } from '@testing-library/react';
import React, { useEffect } from 'react';
import { FormProvider, useForm } from 'react-hook-form';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ActionRequestData, ActionResult } from '../../../actions/Actions.ts';
import { ActionFormContext } from '../Contexts';
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
      isSuccess: undefined,
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
    invalidateForm = false,
    submitForm = false
  }: {
    children: React.ReactNode;
    action?: ActionResult<any, any>;
    defaultValues?: any;
    invalidateForm?: boolean;
    submitForm?: boolean;
  }) => {
    const TestForm = () => {
      const methods = useForm({
        resolver: zodResolver(validationSchema),
        defaultValues,
        mode: 'onBlur'
      });

      useEffect(() => {
        // Simulate form validation state
        if (invalidateForm) {
          methods.setError('atext', { message: 'amessage' });
        }

        // Trigger form submission to set isSubmitted
        if (submitForm) {
          methods.handleSubmit(
            () => {},
            (_errors) => {}
          )();
        }
      }, [invalidateForm, submitForm, methods]);

      return (
        <MemoryRouter>
          <ActionFormContext.Provider value={action}>
            <FormProvider {...methods}>
              <form>{children}</form>
            </FormProvider>
          </ActionFormContext.Provider>
        </MemoryRouter>
      );
    };

    return <TestForm />;
  };

  it('renders with default props', async () => {
    await act(async () =>
      render(
        <FormWrapper>
          <FormSubmitButton id="anid" label="alabel" />
        </FormWrapper>
      )
    );

    const button = screen.getByTestId('anid_form_submit_button');
    expect(button).not.toBeNull();
    expect(button.getAttribute('type')).toBe('submit');
    expect(button.textContent).toBe('alabel');
  });

  it('when no label, renders with default label', async () => {
    await act(async () =>
      render(
        <FormWrapper>
          <FormSubmitButton id="anid" />
        </FormWrapper>
      )
    );

    const button = screen.getByTestId('anid_form_submit_button');
    expect(button.textContent).toBe('Submit');
  });

  it('when no busy Label, and busy, renders with default busy label', async () => {
    await act(async () =>
      render(
        <FormWrapper action={{ ...mockAction, isExecuting: true }}>
          <FormSubmitButton id="anid" />
        </FormWrapper>
      )
    );

    const button = screen.getByTestId('anid_form_submit_button');
    expect(button.textContent).toBe('Sending...');
  });

  it('when busy, renders with busy label', async () => {
    await act(async () =>
      render(
        <FormWrapper action={{ ...mockAction, isExecuting: true }}>
          <FormSubmitButton id="anid" busyLabel="abusylabel" />
        </FormWrapper>
      )
    );

    const button = screen.getByTestId('anid_form_submit_button');
    expect(button.textContent).toBe('abusylabel');
  });

  it('when no complete Label, and completed, renders with default complete label', async () => {
    await act(async () =>
      render(
        <FormWrapper action={{ ...mockAction, isSuccess: true }}>
          <FormSubmitButton id="anid" />
        </FormWrapper>
      )
    );

    const button = screen.getByTestId('anid_form_submit_button');
    expect(button.textContent).toBe('Success!');
  });

  it('when complete, renders with complete label', async () => {
    await act(async () =>
      render(
        <FormWrapper action={{ ...mockAction, isSuccess: true }}>
          <FormSubmitButton id="anid" completeLabel="acompletelabel" />
        </FormWrapper>
      )
    );

    const button = screen.getByTestId('anid_form_submit_button');
    expect(button.textContent).toBe('acompletelabel');
  });

  it('when action is ready, then enabled', async () => {
    await act(async () =>
      render(
        <FormWrapper action={{ ...mockAction, isReady: true }}>
          <FormSubmitButton id="anid" />
        </FormWrapper>
      )
    );

    const button = screen.getByTestId('anid_form_submit_button') as HTMLButtonElement;
    expect(button.disabled).toBe(false);
  });

  it('when not ready, then disabled', async () => {
    await act(async () =>
      render(
        <FormWrapper action={{ ...mockAction, isReady: false }}>
          <FormSubmitButton id="anid" />
        </FormWrapper>
      )
    );

    const button = screen.getByTestId('anid_form_submit_button') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
  });

  it('when form has validation error, then disabled', async () => {
    await act(async () =>
      render(
        <FormWrapper action={{ ...mockAction, isReady: false }} invalidateForm={true} submitForm={true}>
          <FormSubmitButton id="anid" />
        </FormWrapper>
      )
    );

    const button = screen.getByTestId('anid_form_submit_button') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
  });

  it('when submitted successfully, then disabled', async () => {
    await act(async () =>
      render(
        <FormWrapper action={{ ...mockAction, isSuccess: true }} invalidateForm={false} submitForm={true}>
          <FormSubmitButton id="anid" />
        </FormWrapper>
      )
    );

    const button = screen.getByTestId('anid_form_submit_button') as HTMLButtonElement;
    expect(button.disabled).toBe(true);
  });

  it('handles missing action context gracefully', async () => {
    const FormWithoutAction = ({ children }: { children: React.ReactNode }) => {
      const methods = useForm({
        defaultValues: { name: 'John', email: 'john@example.com' }
      });

      return (
        <MemoryRouter>
          <FormProvider {...methods}>
            <form>{children}</form>
          </FormProvider>
        </MemoryRouter>
      );
    };

    await act(async () =>
      render(
        <FormWithoutAction>
          <FormSubmitButton id="anid" />
        </FormWithoutAction>
      )
    );

    const button = screen.getByTestId('anid_form_submit_button') as HTMLButtonElement;
    expect(button).not.toBeNull();
    expect(button.disabled).toBe(true); // Should be disabled when no action
  });
});
