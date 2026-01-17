import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { FormProvider, useForm } from 'react-hook-form';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import Button from '../../button/Button.tsx';
import { FormActionRequiredFieldsContext, FormActionValidationContext } from '../FormActionContexts.tsx';
import FormCheckbox from './FormCheckbox';

vi.mock('../../Components.ts', async (importActual) => {
  const actualImpl = await importActual<typeof import('../../Components.ts')>();

  return {
    ...actualImpl,
    createComponentId: (prefix: string, id: string) => `${prefix}_${id}`
  };
});

describe('FormCheckbox', () => {
  const validationSchema = z.object({
    aname: z.boolean(),
    aRequiredName: z.literal(true, 'Name is required')
  });

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

  it('defaults to unchecked state', () => {
    render(
      <FormWrapper>
        <FormCheckbox id="anid" name="aname" label="alabel" />
      </FormWrapper>
    );

    const checkbox = screen.getByTestId('checkbox_form_checkbox_anid') as HTMLInputElement;
    expect(checkbox.checked).toBe(false);
  });

  it('when default value, sets default value', () => {
    render(
      <FormWrapper defaultValues={{ aname: true }}>
        <FormCheckbox id="anid" name="aname" label="alabel" />
      </FormWrapper>
    );

    const checkbox = screen.getByTestId('checkbox_form_checkbox_anid') as HTMLInputElement;
    expect(checkbox.checked).toBe(true);
  });

  it('handles checkbox state changes', () => {
    render(
      <FormWrapper>
        <FormCheckbox id="anid" name="aname" label="alabel" />
      </FormWrapper>
    );

    const checkbox = screen.getByTestId('checkbox_form_checkbox_anid') as HTMLInputElement;

    expect(checkbox.checked).toBe(false);

    fireEvent.click(checkbox);
    expect(checkbox.checked).toBe(true);

    fireEvent.click(checkbox);
    expect(checkbox.checked).toBe(false);
  });

  it('when changed to invalid values, displays validation error', async () => {
    render(
      <FormWrapper validatesWhen="onChange" mode="onChange">
        <FormCheckbox id="anid" name="aRequiredName" label="alabel" />
      </FormWrapper>
    );

    const checkbox = screen.getByTestId('checkbox_form_checkbox_anid');
    fireEvent.click(checkbox);
    fireEvent.click(checkbox);

    await waitFor(() => {
      const errorMessage = screen.getByTestId('checkbox_form_checkbox_anid_error');
      expect(errorMessage).toBeDefined();
      expect(errorMessage.textContent).toBe('Name is required');
    });
  });

  it('when restored valid values, hides validation error', async () => {
    render(
      <FormWrapper validatesWhen="onChange" mode="onChange">
        <FormCheckbox id="anid" name="aRequiredName" label="alabel" />
      </FormWrapper>
    );

    const checkbox = screen.getByTestId('checkbox_form_checkbox_anid');
    fireEvent.click(checkbox);
    fireEvent.click(checkbox);
    fireEvent.click(checkbox);

    await waitFor(() => expect(screen.queryByTestId('checkbox_form_checkbox_anid_error')).toBeNull());
  });

  it('when validatesWhen is all, shows validation error', async () => {
    render(
      <FormWrapper validatesWhen="all" mode="onChange">
        <FormCheckbox id="anid" name="aRequiredName" label="alabel" />
      </FormWrapper>
    );

    const checkbox = screen.getByTestId('checkbox_form_checkbox_anid');
    fireEvent.click(checkbox);
    fireEvent.click(checkbox);

    await waitFor(() => {
      const errorMessage = screen.getByTestId('checkbox_form_checkbox_anid_error');
      expect(errorMessage.textContent).toBe('Name is required');
    });
  });

  it('when validatesWhen is onBlur, shows validation error immediately', async () => {
    render(
      <FormWrapper validatesWhen="onBlur" mode="onBlur">
        <FormCheckbox id="anid" name="aRequiredName" label="alabel" />
      </FormWrapper>
    );

    const checkbox = screen.getByTestId('checkbox_form_checkbox_anid');
    fireEvent.focus(checkbox);
    fireEvent.blur(checkbox);

    await waitFor(() => {
      const errorMessage = screen.getByTestId('checkbox_form_checkbox_anid_error');
      expect(errorMessage.textContent).toBe('Name is required');
    });
  });

  it('when validatesWhen is onChange, shows validation error after blur', async () => {
    render(
      <FormWrapper validatesWhen="onChange" mode="onChange">
        <FormCheckbox id="anid" name="aRequiredName" label="alabel" />
      </FormWrapper>
    );

    const checkbox = screen.getByTestId('checkbox_form_checkbox_anid');
    fireEvent.click(checkbox);
    fireEvent.click(checkbox);

    await waitFor(() => {
      const errorMessage = screen.getByTestId('checkbox_form_checkbox_anid_error');
      expect(errorMessage.textContent).toBe('Name is required');
    });
  });

  it('when validatesWhen is onSubmit, shows validation error only after form submission', async () => {
    render(
      <FormWrapper validatesWhen="onSubmit" mode="onBlur">
        <FormCheckbox id="anid" name="aRequiredName" label="alabel" />
        <Button id="submit" label="Submit" type="submit" />
      </FormWrapper>
    );

    const checkbox = screen.getByTestId('checkbox_form_checkbox_anid');
    const submitButton = screen.getByTestId('button_submit');

    // Change input but don't submit - should not show error
    fireEvent.click(checkbox);
    fireEvent.click(checkbox);

    expect(screen.queryByTestId('checkbox_form_checkbox_anid_error')).toBeNull();

    fireEvent.click(submitButton);

    await waitFor(() => {
      const errorMessage = screen.getByTestId('checkbox_form_checkbox_anid_error');
      expect(errorMessage.textContent).toBe('Name is required');
    });
  });

  it('handles empty dependencies array', () => {
    render(
      <FormWrapper>
        <FormCheckbox id="anid" name="aname" label="alabel" dependencies={[]} />
      </FormWrapper>
    );

    const input = screen.getByTestId('checkbox_form_checkbox_anid');
    expect(input).not.toBeNull();
  });

  it('handles missing contexts gracefully', () => {
    render(
      <FormWrapperWithoutProviders>
        <FormCheckbox id="anid" name="aname" label="alabel" />
      </FormWrapperWithoutProviders>
    );

    const checkbox = screen.getByTestId('checkbox_form_checkbox_anid');
    expect(checkbox).toBeDefined();
  });

  it('handles nested field names', () => {
    const nestedSchema = z.object({
      parent: z.object({
        child: z.object({
          property: z.boolean()
        })
      })
    });

    const NestedFormWrapper = ({ children }: { children: React.ReactNode }) => {
      const methods = useForm({
        resolver: zodResolver(nestedSchema),
        defaultValues: { parent: { child: { property: true } } }
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
        <FormCheckbox id="anid" name="parent.child.property" label="alabel" />
      </NestedFormWrapper>
    );

    const input = screen.getByTestId('checkbox_form_checkbox_anid') as HTMLInputElement;
    expect(input).not.toBeNull();
    expect(input.value).toBe('on');
  });
});
