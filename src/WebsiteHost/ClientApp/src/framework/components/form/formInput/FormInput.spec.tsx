import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { FormProvider, useForm } from 'react-hook-form';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import Button from '../../button/Button.tsx';
import { FormActionRequiredFieldsContext, FormActionValidationContext } from '../FormActionContexts.tsx';
import FormInput from './FormInput';


vi.mock('../../Components.ts', async (importActual) => {
  const actualImpl = await importActual<typeof import('../../Components.ts')>();
  return {
    ...actualImpl,
    createComponentId: (prefix: string, id: string) => `${prefix}_${id}`
  };
});

describe('FormInput', () => {
  const validationSchema = z.object({
    aname: z.string().min(1, 'Name is required'),
    anEmailAddress: z.email('Invalid email address')
  });

  const FormWrapperWithoutProviders = ({ children }: { children: React.ReactNode }) => {
    const methods = useForm({
      resolver: zodResolver(validationSchema)
    });

    return (
      <FormProvider {...methods}>
        <form>{children}</form>
      </FormProvider>
    );
  };

  const FormWrapper = ({
    children,
    defaultValues = {},
    validatesWhen = 'onBlur',
    requiredFields = [],
    mode = 'onBlur'
  }: {
    children: React.ReactNode;
    defaultValues?: any;
    validatesWhen?: 'onSubmit' | 'onTouched' | 'onBlur' | 'onChange' | 'all';
    requiredFields?: string[];
    mode?: 'onBlur' | 'onChange' | 'onSubmit' | 'onTouched' | 'all';
  }) => {
    const TestForm = () => {
      const methods = useForm({
        resolver: zodResolver(validationSchema),
        defaultValues,
        mode
      });

      return (
        <MemoryRouter>
          <FormActionRequiredFieldsContext.Provider value={requiredFields}>
            <FormActionValidationContext.Provider value={validatesWhen}>
              <FormProvider {...methods}>
                <form onSubmit={methods.handleSubmit(() => {})}>{children}</form>
              </FormProvider>
            </FormActionValidationContext.Provider>
          </FormActionRequiredFieldsContext.Provider>
        </MemoryRouter>
      );
    };

    return <TestForm />;
  };

  it('renders with default text type', () => {
    render(
      <FormWrapper>
        <FormInput id="anid" name="aname" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_anid');
    expect(input.getAttribute('type')).toBe('text');
  });

  it('when is in required list, displays required', () => {
    render(
      <FormWrapper requiredFields={['aname']}>
        <FormInput id="anid" name="aname" label="alabel" />
      </FormWrapper>
    );

    const requiredIndicator = screen.getByTestId('input_form_input_anid_required');
    expect(requiredIndicator.textContent).toBe('*');
  });

  it('when not required, does not display required', () => {
    render(
      <FormWrapper requiredFields={[]}>
        <FormInput id="anid" name="aname" label="alabel" />
      </FormWrapper>
    );

    const requiredIndicator = screen.queryByTestId('input_form_input_anid_required');
    expect(requiredIndicator).toBeNull();
  });

  it('when default values, sets default value', async () => {
    render(
      <FormWrapper defaultValues={{ aname: 'John' }}>
        <FormInput id="anid" name="aname" label="alabel" />
      </FormWrapper>
    );

    const input = screen.queryByTestId('input_form_input_anid') as HTMLInputElement;
    expect(input.value).toBe('John');
  });

  it('when no default values, displays no validation error', () => {
    render(
      <FormWrapper>
        <FormInput id="anid" name="aname" label="alabel" />
      </FormWrapper>
    );

    expect(screen.queryByTestId('input_form_input_anid_error')).toBeNull();
  });

  it('when changed to invalid values, displays validation error', async () => {
    render(
      <FormWrapper defaultValues={{ aname: 'John' }} validatesWhen="onChange" mode="onChange">
        <FormInput id="anid" name="aname" label="alabel" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_anid');
    fireEvent.change(input, { target: { value: '' } });

    await waitFor(() => {
      const errorMessage = screen.getByTestId('input_form_input_anid_error');
      expect(errorMessage).toBeDefined();
      expect(errorMessage.textContent).toBe('Name is required');
    });
  });

  it('when restored valid values, hides validation error', async () => {
    render(
      <FormWrapper>
        <FormInput id="anid" name="aname" label="alabel" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_anid');
    fireEvent.change(input, { target: { value: 'John' } });

    await waitFor(() => expect(screen.queryByTestId('input_form_input_anid_error')).toBeNull());
  });

  it('when validatesWhen is all, shows validation error', async () => {
    render(
      <FormWrapper validatesWhen="all" mode="onChange">
        <FormInput id="anid" name="anEmailAddress" type="email" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_anid');
    fireEvent.change(input, { target: { value: 'aninvalidemailaddress' } });
    fireEvent.change(input, { target: { value: '' } });

    await waitFor(() => {
      const errorMessage = screen.getByTestId('input_form_input_anid_error');
      expect(errorMessage.textContent).toBe('Invalid email address');
    });
  });

  it('when validatesWhen is onChange, shows validation error immediately', async () => {
    render(
      <FormWrapper validatesWhen="onChange" mode="onChange">
        <FormInput id="anid" name="anEmailAddress" type="email" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_anid');
    fireEvent.change(input, { target: { value: 'aninvalidemailaddress' } });

    await waitFor(() => {
      const errorMessage = screen.getByTestId('input_form_input_anid_error');
      expect(errorMessage.textContent).toBe('Invalid email address');
    });
  });

  it('when validatesWhen is onBlur, shows validation error after blur', async () => {
    render(
      <FormWrapper validatesWhen="onBlur" mode="onBlur">
        <FormInput id="anid" name="anEmailAddress" type="email" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_anid');
    fireEvent.change(input, { target: { value: 'aninvalidemailaddress' } });
    fireEvent.blur(input);

    await waitFor(() => {
      const errorMessage = screen.getByTestId('input_form_input_anid_error');
      expect(errorMessage.textContent).toBe('Invalid email address');
    });
  });

  it('when validatesWhen is onTouched, shows validation error after touch', async () => {
    render(
      <FormWrapper validatesWhen="onTouched" mode="onTouched">
        <FormInput id="anid" name="anEmailAddress" type="email" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_anid');
    fireEvent.focus(input);
    fireEvent.blur(input);

    await waitFor(() => {
      const errorMessage = screen.getByTestId('input_form_input_anid_error');
      expect(errorMessage.textContent).toBe('Invalid email address');
    });
  });

  it('when validatesWhen is onSubmit, shows validation error only after form submission', async () => {
    render(
      <FormWrapper validatesWhen="onSubmit" mode="onBlur">
        <FormInput id="anid" name="anEmailAddress" type="email" />
        <Button id="submit" label="Submit" type="submit" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_anid');
    const submitButton = screen.getByTestId('button_submit');

    // Change input but don't submit - should not show error
    fireEvent.change(input, { target: { value: '' } });
    fireEvent.blur(input);

    expect(screen.queryByTestId('input_form_input_anid_error')).toBeNull();

    fireEvent.click(submitButton);

    await waitFor(() => {
      const errorMessage = screen.getByTestId('input_form_input_anid_error');
      expect(errorMessage.textContent).toBe('Invalid email address');
    });
  });

  it('handles empty dependencies array', () => {
    render(
      <FormWrapper>
        <FormInput id="anid" name="aname" dependencies={[]} />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_anid');
    expect(input).not.toBeNull();
  });

  it('handles missing contexts gracefully', () => {
    render(
      <FormWrapperWithoutProviders>
        <FormInput id="anid" name="aname" />
      </FormWrapperWithoutProviders>
    );

    const input = screen.getByTestId('input_form_input_anid');
    expect(input).not.toBeNull();
  });

  it('handles nested field names', () => {
    const nestedSchema = z.object({
      parent: z.object({
        child: z.object({
          property: z.string().min(1, 'Name is required')
        })
      })
    });

    const NestedFormWrapper = ({ children }: { children: React.ReactNode }) => {
      const methods = useForm({
        resolver: zodResolver(nestedSchema),
        defaultValues: { parent: { child: { property: 'avalue' } } }
      });

      return (
        <FormActionRequiredFieldsContext.Provider value={[]}>
          <FormActionValidationContext.Provider value="onChange">
            <FormProvider {...methods}>
              <form>{children}</form>
            </FormProvider>
          </FormActionValidationContext.Provider>
        </FormActionRequiredFieldsContext.Provider>
      );
    };

    render(
      <NestedFormWrapper>
        <FormInput id="anid" name="parent.child.property" />
      </NestedFormWrapper>
    );

    const input = screen.getByTestId('input_form_input_anid') as HTMLInputElement;
    expect(input).not.toBeNull();
    expect(input.value).toBe('avalue');
  });
});
