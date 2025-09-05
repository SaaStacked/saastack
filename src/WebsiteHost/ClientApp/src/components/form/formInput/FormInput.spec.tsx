import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { FormProvider, useForm } from 'react-hook-form';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import Button from '../../button/Button.tsx';
import { ActionFormRequiredFieldsContext, ActionFromValidationContext } from '../Contexts';
import FormInput, { getNestedField } from './FormInput';

vi.mock('../../Components.ts', () => ({
  createComponentId: (prefix: string, id: string) => `${prefix}_${id}`
}));

describe('FormInput', () => {
  const validationSchema = z.object({
    name: z.string().min(1, 'Name is required'),
    email: z.email('Invalid email address'),
    age: z.number().min(18, 'Must be at least 18'),
    password: z.string().min(6, 'Password must be at least 6 characters')
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
          <ActionFormRequiredFieldsContext.Provider value={requiredFields}>
            <ActionFromValidationContext.Provider value={validatesWhen}>
              <FormProvider {...methods}>
                <form onSubmit={methods.handleSubmit(() => {})}>{children}</form>
              </FormProvider>
            </ActionFromValidationContext.Provider>
          </ActionFormRequiredFieldsContext.Provider>
        </MemoryRouter>
      );
    };

    return <TestForm />;
  };

  it('renders with default text type', () => {
    render(
      <FormWrapper>
        <FormInput id="name" name="name" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_name');
    expect(input.getAttribute('type')).toBe('text');
  });

  it('when is in required list, displays required', () => {
    render(
      <FormWrapper requiredFields={['name']}>
        <FormInput id="name" name="name" label="Name" />
      </FormWrapper>
    );

    const requiredIndicator = screen.getByTestId('input_form_input_name_required');
    expect(requiredIndicator.textContent).toBe('*');
  });

  it('when not required, does not display required', () => {
    render(
      <FormWrapper requiredFields={[]}>
        <FormInput id="name" name="name" label="Name" />
      </FormWrapper>
    );

    const requiredIndicator = screen.queryByTestId('input_form_input_name_required');
    expect(requiredIndicator).toBeNull();
  });

  it('when default values, sets values', async () => {
    render(
      <FormWrapper defaultValues={{ name: 'John' }}>
        <FormInput id="name" name="name" label="Name" />
      </FormWrapper>
    );

    const input = screen.queryByTestId('input_form_input_name') as HTMLInputElement;
    expect(input.value).toBe('John');
  });

  it('when no default values, displays no validation error', () => {
    render(
      <FormWrapper>
        <FormInput id="name" name="name" label="Name" />
      </FormWrapper>
    );

    expect(screen.queryByTestId('input_form_input_name_error')).toBeNull();
  });

  it('when changed to invalid values, displays validation error', async () => {
    render(
      <FormWrapper defaultValues={{ name: 'John' }} validatesWhen="onChange" mode="onChange">
        <FormInput id="name" name="name" label="Name" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_name');
    fireEvent.change(input, { target: { value: '' } });

    await waitFor(() => {
      const errorMessage = screen.getByTestId('input_form_input_name_error');
      expect(errorMessage).toBeDefined();
      expect(errorMessage.textContent).toBe('Name is required');
    });
  });

  it('when restored valid values, hides validation error', async () => {
    render(
      <FormWrapper>
        <FormInput id="name" name="name" label="Name" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_name');
    fireEvent.change(input, { target: { value: 'John' } });

    await waitFor(() => expect(screen.queryByTestId('input_form_input_name_error')).toBeNull());
  });

  it('when validatesWhen is all, shows validation error', async () => {
    render(
      <FormWrapper validatesWhen="all" mode="onChange">
        <FormInput id="name" name="name" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_name');
    fireEvent.change(input, { target: { value: 'valid name' } });
    fireEvent.change(input, { target: { value: '' } });

    await waitFor(() => {
      const errorMessage = screen.getByTestId('input_form_input_name_error');
      expect(errorMessage.textContent).toBe('Name is required');
    });
  });

  it('when validatesWhen is onChange, shows validation error immediately', async () => {
    render(
      <FormWrapper validatesWhen="onChange" mode="onChange">
        <FormInput id="email" name="email" type="email" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_email');
    fireEvent.change(input, { target: { value: 'invalid-email' } });

    await waitFor(() => {
      const errorMessage = screen.getByTestId('input_form_input_email_error');
      expect(errorMessage.textContent).toBe('Invalid email address');
    });
  });

  it('when validatesWhen is onBlur, shows validation error after blur when validatesWhen is onBlur', async () => {
    render(
      <FormWrapper validatesWhen="onBlur" mode="onBlur">
        <FormInput id="email" name="email" type="email" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_email');
    fireEvent.change(input, { target: { value: 'invalid-email' } });
    fireEvent.blur(input);

    await waitFor(() => {
      const errorMessage = screen.getByTestId('input_form_input_email_error');
      expect(errorMessage.textContent).toBe('Invalid email address');
    });
  });

  it('when validatesWhen is onTouched, shows validation error after touch', async () => {
    render(
      <FormWrapper validatesWhen="onTouched" mode="onTouched">
        <FormInput id="name" name="name" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_name');
    fireEvent.focus(input);
    fireEvent.blur(input);

    await waitFor(() => {
      const errorMessage = screen.getByTestId('input_form_input_name_error');
      expect(errorMessage.textContent).toBe('Name is required');
    });
  });

  it('when validatesWhen is onSubmit, shows validation error only after form submission', async () => {
    render(
      <FormWrapper validatesWhen="onSubmit" mode="onBlur">
        <FormInput id="name" name="name" />
        <Button id="submit" label="Submit" type="submit" />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_name');
    const submitButton = screen.getByTestId('button_submit');

    // Change input but don't submit - should not show error
    fireEvent.change(input, { target: { value: '' } });
    fireEvent.blur(input);

    expect(screen.queryByTestId('input_form_input_name-error')).toBeNull();

    fireEvent.click(submitButton);

    await waitFor(() => {
      const errorMessage = screen.getByTestId('input_form_input_name_error');
      expect(errorMessage.textContent).toBe('Name is required');
    });
  });

  it('registers field with dependencies', () => {
    render(
      <FormWrapper>
        <FormInput id="confirm-password" name="confirmPassword" dependencies={['password']} />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_confirm-password');
    expect(input).not.toBeNull();
  });

  it('handles empty dependencies array', () => {
    render(
      <FormWrapper>
        <FormInput id="name" name="name" dependencies={[]} />
      </FormWrapper>
    );

    const input = screen.getByTestId('input_form_input_name');
    expect(input).not.toBeNull();
  });

  it('handles missing contexts gracefully', () => {
    render(
      <FormWrapperWithoutProviders>
        <FormInput id="name" name="name" />
      </FormWrapperWithoutProviders>
    );

    const input = screen.getByTestId('input_form_input_name');
    expect(input).not.toBeNull();
  });

  it('handles nested field names', () => {
    const nestedSchema = z.object({
      user: z.object({
        profile: z.object({
          name: z.string().min(1, 'Name is required')
        })
      })
    });

    const NestedFormWrapper = ({ children }: { children: React.ReactNode }) => {
      const methods = useForm({
        resolver: zodResolver(nestedSchema),
        defaultValues: { user: { profile: { name: '' } } }
      });

      return (
        <ActionFormRequiredFieldsContext.Provider value={[]}>
          <ActionFromValidationContext.Provider value="onChange">
            <FormProvider {...methods}>
              <form>{children}</form>
            </FormProvider>
          </ActionFromValidationContext.Provider>
        </ActionFormRequiredFieldsContext.Provider>
      );
    };

    render(
      <NestedFormWrapper>
        <FormInput id="nested-name" name="user.profile.name" />
      </NestedFormWrapper>
    );

    const input = screen.getByTestId('input_form_input_nested-name');
    expect(input).not.toBeNull();
  });
});

describe('getNestedField', () => {
  it('returns value for simple property', () => {
    const obj = { name: 'John' };
    expect(getNestedField(obj, 'name')).toBe('John');
  });

  it('returns value for nested property', () => {
    const obj = { user: { profile: { name: 'John' } } };
    expect(getNestedField(obj, 'user.profile.name')).toBe('John');
  });

  it('returns undefined for non-existent property', () => {
    const obj = { name: 'John' };
    expect(getNestedField(obj, 'age')).toBeUndefined();
  });

  it('returns undefined for non-existent nested property', () => {
    const obj = { user: { name: 'John' } };
    expect(getNestedField(obj, 'user.profile.age')).toBeUndefined();
  });

  it('when object is null/undefined, returns undefined', () => {
    expect(getNestedField(null, 'name')).toBeUndefined();
    expect(getNestedField(undefined, 'name')).toBeUndefined();
  });

  it('when path is empty path, returns object', () => {
    const obj = { name: 'John' };
    expect(getNestedField(obj, '')).toBe(obj);
  });

  it('handles deeply nested objects', () => {
    const obj = { a: { b: { c: { d: { e: 'deep value' } } } } };
    expect(getNestedField(obj, 'a.b.c.d.e')).toBe('deep value');
  });

  it('handles arrays in nested path', () => {
    const obj = { users: [{ name: 'John' }, { name: 'Jane' }] };
    expect(getNestedField(obj, 'users.0.name')).toBe('John');
    expect(getNestedField(obj, 'users.1.name')).toBe('Jane');
  });
});
