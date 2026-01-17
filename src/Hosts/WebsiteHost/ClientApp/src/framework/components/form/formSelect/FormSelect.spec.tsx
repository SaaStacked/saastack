import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { FormProvider, useForm } from 'react-hook-form';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it, vi } from 'vitest';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import Button from '../../button/Button.tsx';
import { FormActionRequiredFieldsContext, FormActionValidationContext } from '../FormActionContexts.tsx';
import FormSelect from './FormSelect';

vi.mock('../../Components.ts', async (importActual) => {
  const actualImpl = await importActual<typeof import('../../Components.ts')>();
  return {
    ...actualImpl,
    createComponentId: (prefix: string, id: string) => `${prefix}_${id}`
  };
});

const options = [
  { value: 'anoption1', label: 'avalue1' },
  { value: 'anoption2', label: 'avalue2' },
  { value: 'anoption3', label: 'avalue3' }
];

describe('FormSelect', () => {
  const validationSchema = z.object({
    aname: z.string().min(1, 'Selection is required'),
    acountry: z.string().min(1, 'Country is required')
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

  it('renders with default props', () => {
    render(
      <FormWrapper>
        <FormSelect id="anid" name="aname" options={options} />
      </FormWrapper>
    );

    const select = screen.getByTestId('select_form_select_anid');
    expect(select).toBeDefined();
    expect(select.tagName).toBe('SELECT');
  });

  it('when is in required list, displays required', () => {
    render(
      <FormWrapper requiredFields={['aname']}>
        <FormSelect id="anid" name="aname" label="alabel" options={options} />
      </FormWrapper>
    );

    const requiredIndicator = screen.getByTestId('select_form_select_anid_required');
    expect(requiredIndicator.textContent).toBe('*');
  });

  it('when not required, does not display required', () => {
    render(
      <FormWrapper requiredFields={[]}>
        <FormSelect id="anid" name="aname" label="alabel" options={options} />
      </FormWrapper>
    );

    const requiredIndicator = screen.queryByTestId('select_form_select_anid_required');
    expect(requiredIndicator).toBeNull();
  });

  it('when default values, sets default value', async () => {
    render(
      <FormWrapper defaultValues={{ aname: 'anoption2' }}>
        <FormSelect id="anid" name="aname" label="alabel" options={options} />
      </FormWrapper>
    );

    const select = screen.queryByTestId('select_form_select_anid') as HTMLSelectElement;
    expect(select.value).toBe('anoption2');
  });

  it('when no default values, displays no validation error', () => {
    render(
      <FormWrapper>
        <FormSelect id="anid" name="aname" label="alabel" options={options} />
      </FormWrapper>
    );

    expect(screen.queryByTestId('select_form_select_anid_error')).toBeNull();
  });

  it('when changed to invalid values, displays validation error', async () => {
    render(
      <FormWrapper defaultValues={{ aname: 'anoption1' }} validatesWhen="onChange" mode="onChange">
        <FormSelect id="anid" name="aname" label="alabel" options={options} />
      </FormWrapper>
    );

    const select = screen.getByTestId('select_form_select_anid');
    fireEvent.change(select, { target: { value: '' } });

    await waitFor(() => {
      const errorMessage = screen.getByTestId('select_form_select_anid_error');
      expect(errorMessage).toBeDefined();
      expect(errorMessage.textContent).toBe('Selection is required');
    });
  });

  it('when restored valid values, hides validation error', async () => {
    render(
      <FormWrapper>
        <FormSelect id="anid" name="aname" label="alabel" options={options} />
      </FormWrapper>
    );

    const select = screen.getByTestId('select_form_select_anid');
    fireEvent.change(select, { target: { value: 'anoption1' } });

    await waitFor(() => expect(screen.queryByTestId('select_form_select_anid_error')).toBeNull());
  });

  it('when validatesWhen is all, shows validation error', async () => {
    const countryOptions = [
      { value: 'us', label: 'United States' },
      { value: 'ca', label: 'Canada' },
      { value: 'uk', label: 'United Kingdom' }
    ];

    render(
      <FormWrapper validatesWhen="all" mode="onChange">
        <FormSelect id="anid" name="acountry" options={countryOptions} />
      </FormWrapper>
    );

    const select = screen.getByTestId('select_form_select_anid');
    fireEvent.change(select, { target: { value: 'invalid' } });
    fireEvent.change(select, { target: { value: '' } });

    await waitFor(() => {
      const errorMessage = screen.getByTestId('select_form_select_anid_error');
      expect(errorMessage.textContent).toBe('Country is required');
    });
  });

  it('when validatesWhen is onChange, shows validation error immediately', async () => {
    const countryOptions = [
      { value: 'us', label: 'United States' },
      { value: 'ca', label: 'Canada' },
      { value: 'uk', label: 'United Kingdom' }
    ];

    render(
      <FormWrapper validatesWhen="onChange" mode="onChange">
        <FormSelect id="anid" name="acountry" options={countryOptions} />
      </FormWrapper>
    );

    const select = screen.getByTestId('select_form_select_anid');
    fireEvent.change(select, { target: { value: 'invalid' } });

    await waitFor(() => {
      const errorMessage = screen.getByTestId('select_form_select_anid_error');
      expect(errorMessage.textContent).toBe('Country is required');
    });
  });

  it('when validatesWhen is onBlur, shows validation error after blur', async () => {
    const countryOptions = [
      { value: 'us', label: 'United States' },
      { value: 'ca', label: 'Canada' },
      { value: 'uk', label: 'United Kingdom' }
    ];

    render(
      <FormWrapper validatesWhen="onBlur" mode="onBlur">
        <FormSelect id="anid" name="acountry" options={countryOptions} />
      </FormWrapper>
    );

    const select = screen.getByTestId('select_form_select_anid');
    fireEvent.change(select, { target: { value: 'invalid' } });
    fireEvent.blur(select);

    await waitFor(() => {
      const errorMessage = screen.getByTestId('select_form_select_anid_error');
      expect(errorMessage.textContent).toBe('Country is required');
    });
  });

  it('when validatesWhen is onTouched, shows validation error after touch', async () => {
    const countryOptions = [
      { value: 'us', label: 'United States' },
      { value: 'ca', label: 'Canada' },
      { value: 'uk', label: 'United Kingdom' }
    ];

    render(
      <FormWrapper defaultValues={{ acountry: '' }} validatesWhen="onTouched" mode="onTouched">
        <FormSelect id="anid" name="acountry" options={countryOptions} />
      </FormWrapper>
    );

    const select = screen.getByTestId('select_form_select_anid');
    fireEvent.focus(select);
    fireEvent.blur(select);

    await waitFor(() => {
      const errorMessage = screen.getByTestId('select_form_select_anid_error');
      expect(errorMessage.textContent).toBe('Country is required');
    });
  });

  it('when validatesWhen is onSubmit, shows validation error only after form submission', async () => {
    const countryOptions = [
      { value: 'us', label: 'United States' },
      { value: 'ca', label: 'Canada' },
      { value: 'uk', label: 'United Kingdom' }
    ];

    render(
      <FormWrapper validatesWhen="onSubmit" mode="onBlur">
        <FormSelect id="anid" name="acountry" options={countryOptions} />
        <Button id="submit" label="Submit" type="submit" />
      </FormWrapper>
    );

    const select = screen.getByTestId('select_form_select_anid');
    const submitButton = screen.getByTestId('button_submit');

    // Change select but don't submit - should not show error
    fireEvent.change(select, { target: { value: '' } });
    fireEvent.blur(select);

    expect(screen.queryByTestId('select_form_select_anid_error')).toBeNull();

    fireEvent.click(submitButton);

    await waitFor(() => {
      const errorMessage = screen.getByTestId('select_form_select_anid_error');
      expect(errorMessage.textContent).toBe('Country is required');
    });
  });

  it('handles empty dependencies array', () => {
    render(
      <FormWrapper>
        <FormSelect id="anid" name="aname" options={options} dependencies={[]} />
      </FormWrapper>
    );

    const select = screen.getByTestId('select_form_select_anid');
    expect(select).not.toBeNull();
  });

  it('handles missing contexts gracefully', () => {
    render(
      <FormWrapperWithoutProviders>
        <FormSelect id="anid" name="aname" options={options} />
      </FormWrapperWithoutProviders>
    );

    const select = screen.getByTestId('select_form_select_anid');
    expect(select).not.toBeNull();
  });

  it('handles nested field names', () => {
    const nestedSchema = z.object({
      parent: z.object({
        child: z.object({
          property: z.string().min(1, 'Selection is required')
        })
      })
    });

    const NestedFormWrapper = ({ children }: { children: React.ReactNode }) => {
      const methods = useForm({
        resolver: zodResolver(nestedSchema),
        defaultValues: { parent: { child: { property: 'anoption2' } } }
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
        <FormSelect id="anid" name="parent.child.property" options={options} />
      </NestedFormWrapper>
    );

    const select = screen.getByTestId('select_form_select_anid') as HTMLSelectElement;
    expect(select).not.toBeNull();
    expect(select.value).toBe('anoption2');
  });

  it('renders all options correctly', () => {
    render(
      <FormWrapper>
        <FormSelect id="anid" name="aname" options={options} />
      </FormWrapper>
    );

    options.forEach((option) => expect(screen.getByText(option.label)).toBeDefined());
  });

  it('renders placeholder when provided', () => {
    render(
      <FormWrapper>
        <FormSelect id="anid" name="aname" options={options} placeholder="Choose an option" />
      </FormWrapper>
    );

    expect(screen.getByText('Choose an option')).toBeDefined();
  });
});
