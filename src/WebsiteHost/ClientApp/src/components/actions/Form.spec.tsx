import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { useFormContext } from 'react-hook-form';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { z } from 'zod';
import { ActionRequestData, ActionResult } from '../../actions/Actions';
import Form, { getRequiredFields } from './Form';
import FormSubmitButton from './formSubmitButton/FormSubmitButton.tsx';

vi.mock('./Contexts', () => ({
  ActionContext: {
    Provider: ({ children }: any) => <div data-testid="action-context">{children}</div>
  },
  FormValidationContext: {
    Provider: ({ children }: any) => <div data-testid="form-validation-context">{children}</div>
  },
  RequiredFieldsContext: {
    Provider: ({ children }: any) => <div data-testid="required-fields-context">{children}</div>
  }
}));

vi.mock('../alert/Alert', () => ({
  default: ({ id, message, type }: any) =>
    message ? (
      <div data-testid={id} className={`alert-${type}`}>
        {message}
      </div>
    ) : null
}));

vi.mock('../error/UnhandledError', () => ({
  default: ({ id, error }: any) => (error ? <div data-testid={id}>anunhandlederror</div> : null)
}));

interface TestRequestData extends ActionRequestData {
  atext: string;
  anemailaddress: string;
}

describe('Form', () => {
  let mockAction: ActionResult<TestRequestData, 'A_VALIDATION_ERROR', any>;
  const validationSchema = z.object({
    atext: z.string().min(1, 'amessage1'),
    anemailaddress: z.email('amessage2')
  });

  const renderWithRouter = (component: React.ReactElement) => render(<MemoryRouter>{component}</MemoryRouter>);

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

  it('renders form with correct test id and classes', () => {
    render(
      <Form id="anid" action={mockAction} validations={validationSchema}>
        <input name="atext" />
      </Form>
    );

    const form = screen.getByTestId('anid_action_form');
    expect(form).not.toBeNull();
    expect(form.tagName).toBe('FORM');
    expect(form.className).toContain('bg-white');
    expect(form.className).toContain('rounded-lg');
    expect(form.className).toContain('transition-all');
  });

  it('applies custom className', () => {
    render(
      <Form id="anid" className="aclassname" action={mockAction} validations={validationSchema}>
        <input name="atext" />
      </Form>
    );

    const form = screen.getByTestId('anid_action_form');
    expect(form.className).toContain('aclassname');
  });

  it('sets form name attribute correctly', () => {
    render(
      <Form id="anid" action={mockAction} validations={validationSchema}>
        <input name="atext" />
      </Form>
    );

    const form = screen.getByTestId('anid_action_form');
    expect(form.getAttribute('name')).toBe('anid_action_form');
  });

  it('sets JavaScript noValidate attribute', () => {
    render(
      <Form id="anid" action={mockAction} validations={validationSchema}>
        <input name="atext" />
      </Form>
    );

    const form = screen.getByTestId('anid_action_form');
    expect(form.hasAttribute('noValidate')).toBe(true);
  });

  it('applies default values correctly', () => {
    const defaultValues = { atext: 'aname', anemailaddress: 'auser@company.com' };

    const TestInputs = () => {
      const { register } = useFormContext();
      return (
        <>
          <input data-testid="input1" {...register('atext')} />
          <input data-testid="input2" {...register('anemailaddress')} />
        </>
      );
    };

    render(
      <Form id="anid" action={mockAction} validations={validationSchema} defaultValues={defaultValues}>
        <TestInputs />
      </Form>
    );

    const form = screen.getByTestId('anid_action_form');
    expect(form).not.toBeNull();
    expect((screen.getByTestId('input1') as HTMLInputElement).value).toBe('aname');
    expect((screen.getByTestId('input2') as HTMLInputElement).value).toBe('auser@company.com');
  });

  it('calls execute on form submission', async () => {
    mockAction.execute = vi.fn();
    const defaultValues = { atext: 'aname', anemailaddress: 'auser@company.com' };

    const TestInputs = () => {
      const { register } = useFormContext();
      return (
        <>
          <input {...register('atext')} />
          <input {...register('anemailaddress')} />
        </>
      );
    };

    renderWithRouter(
      <Form id="anid" action={mockAction} validations={validationSchema} defaultValues={defaultValues}>
        <TestInputs />
        <FormSubmitButton id="submit" />
      </Form>
    );

    const form = screen.getByTestId('anid_action_form');
    fireEvent.submit(form);

    await waitFor(() => expect(mockAction.execute).toHaveBeenCalled());
  });

  it('calls onSuccess callback when action succeeds', async () => {
    const onSuccess = vi.fn();
    mockAction.execute = vi.fn((formData, options) =>
      options?.onSuccess?.({
        requestData: formData,
        response: { success: true }
      })
    );
    const defaultValues = { atext: 'aname', anemailaddress: 'auser@company.com' };

    const TestInputs = () => {
      const { register } = useFormContext();
      return (
        <>
          <input {...register('atext')} />
          <input {...register('anemailaddress')} />
        </>
      );
    };

    renderWithRouter(
      <Form
        id="anid"
        action={mockAction}
        validations={validationSchema}
        defaultValues={defaultValues}
        onSuccess={onSuccess}
      >
        <TestInputs />
        <FormSubmitButton id="submit" />
      </Form>
    );

    const form = screen.getByTestId('anid_action_form');
    fireEvent.submit(form);

    await waitFor(() =>
      expect(onSuccess).toHaveBeenCalledWith({
        requestData: { atext: 'aname', anemailaddress: 'auser@company.com' },
        response: { success: true }
      })
    );
  });

  it('displays expected error message when action fails', () => {
    mockAction.lastExpectedError = { code: 'A_VALIDATION_ERROR' as any };
    const expectedErrorMessages = { A_VALIDATION_ERROR: 'amessage' };

    render(
      <Form id="anid" action={mockAction} validations={validationSchema} expectedErrorMessages={expectedErrorMessages}>
        <input name="atext" />
      </Form>
    );

    const errorAlert = screen.getByTestId('anid_action_form_expected_error');
    expect(errorAlert).not.toBeNull();
    expect(errorAlert.textContent).toBe('amessage');
    expect(errorAlert.className).toContain('alert-error');
  });

  it('displays unhandled error when action has unexpected error', () => {
    mockAction.lastUnexpectedError = new Error('anerror') as any;

    render(
      <Form id="anid" action={mockAction} validations={validationSchema}>
        <input name="atext" />
      </Form>
    );

    const errorComponent = screen.getByTestId('anid_action_form_unexpected_error');
    expect(errorComponent).not.toBeNull();
    expect(errorComponent.textContent).toBe('anunhandlederror');
  });

  it('does not display error alert when no expected error', () => {
    render(
      <Form id="anid" action={mockAction} validations={validationSchema}>
        <input name="atext" />
      </Form>
    );

    const errorAlert = screen.queryByTestId('anid_action_form_expected_error');
    expect(errorAlert).toBeNull();
  });

  it('uses default validatesWhen as onBlur', () => {
    render(
      <Form id="anid" action={mockAction} validations={validationSchema}>
        <input name="atext" />
      </Form>
    );

    // Form should render without errors (default validatesWhen is applied)
    const form = screen.getByTestId('anid_action_form');
    expect(form).not.toBeNull();
  });

  it('accepts custom validatesWhen prop', () => {
    render(
      <Form id="anid" action={mockAction} validations={validationSchema} validatesWhen="onChange">
        <input name="atext" />
      </Form>
    );

    const form = screen.getByTestId('anid_action_form');
    expect(form).not.toBeNull();
  });

  it('handles form without validations', () => {
    render(
      <Form id="anid" action={mockAction} validations={undefined as any}>
        <input name="atext" />
      </Form>
    );

    const form = screen.getByTestId('anid_action_form');
    expect(form).not.toBeNull();
  });
});

describe('getRequiredFields', () => {
  it('when schema is empty, returns no required', () => {
    const schema = z.object({});
    const requiredFields = getRequiredFields(schema);
    expect(requiredFields).toEqual([]);
  });

  it('when all optional, returns none', () => {
    const schema = z.object({
      atext: z.string().optional(),
      anemailaddress: z.string().optional()
    });

    const requiredFields = getRequiredFields(schema);
    expect(requiredFields).toEqual([]);
  });

  it('when some required and some optional, returns required fields only', () => {
    const schema = z.object({
      atext: z.string(),
      anemailaddress: z.email(),
      age: z.number().optional()
    });

    const requiredFields = getRequiredFields(schema);
    expect(requiredFields).toEqual(['atext', 'anemailaddress']);
  });
});
