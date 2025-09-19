import { fireEvent, render, screen, waitFor } from '@testing-library/react';
import React from 'react';
import { MemoryRouter } from 'react-router-dom';
import { beforeEach, describe, expect, it, vi } from 'vitest';
import { z } from 'zod';
import { ActionRequestData, ActionResult } from '../../actions/Actions.ts';
import Form, { getRequiredFields } from './Form';
import FormInput from './formInput/FormInput.tsx';
import FormSubmitButton from './formSubmitButton/FormSubmitButton.tsx';


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

  it('applies custom className', () => {
    render(
      <Form id="anid" className="aclassname" action={mockAction} validationSchema={validationSchema}>
        <FormInput id="anid1" name="atext" label="Name" />
      </Form>
    );

    const form = screen.getByTestId('anid_action_form');
    expect(form.className).toContain('aclassname');
  });

  it('sets JavaScript noValidate attribute', () => {
    render(
      <Form id="anid" action={mockAction} validationSchema={validationSchema}>
        <FormInput id="anid1" name="atext" label="Name" />
      </Form>
    );

    const form = screen.getByTestId('anid_action_form');
    expect(form.hasAttribute('noValidate')).toBe(true);
  });

  it('applies default values correctly', () => {
    const defaultValues = { atext: 'aname', anemailaddress: 'auser@company.com' };
    render(
      <Form id="anid" action={mockAction} validationSchema={validationSchema} defaultValues={defaultValues}>
        <FormInput id="anid1" name="atext" label="Name" />
        <FormInput id="anid2" name="anemailaddress" label="Email" />
      </Form>
    );

    expect((screen.getByTestId('anid1_form_input_input') as HTMLInputElement).value).toBe('aname');
    expect((screen.getByTestId('anid2_form_input_input') as HTMLInputElement).value).toBe('auser@company.com');
  });

  it('when form submitted, calls execute', async () => {
    mockAction.execute = vi.fn();
    const defaultValues = { atext: 'aname', anemailaddress: 'auser@company.com' };

    renderWithRouter(
      <Form id="anid" action={mockAction} validationSchema={validationSchema} defaultValues={defaultValues}>
        <FormInput id="anid1" name="atext" label="Name" />
        <FormInput id="anid2" name="anemailaddress" label="Email" />
        <FormSubmitButton id="submit" />
      </Form>
    );

    const form = screen.getByTestId('anid_action_form');
    fireEvent.submit(form);

    await waitFor(() => expect(mockAction.execute).toHaveBeenCalled());
    expect(mockAction.execute).toHaveBeenCalledWith(
      { atext: 'aname', anemailaddress: 'auser@company.com' },
      expect.anything()
    );
    const expectedError = screen.queryByTestId('anid_action_form_expected_error');
    expect(expectedError).toBeNull();
    const unexpectedError = screen.queryByTestId('anid_action_form_unexpected_error');
    expect(unexpectedError).toBeNull();
  });

  it('when execute succeeds, calls onSuccess callback', async () => {
    const onSuccess = vi.fn();
    mockAction.execute = vi.fn((formData, options) =>
      options?.onSuccess?.({
        requestData: formData,
        response: { success: true }
      })
    );
    const defaultValues = { atext: 'aname', anemailaddress: 'auser@company.com' };

    renderWithRouter(
      <Form
        id="anid"
        action={mockAction}
        validationSchema={validationSchema}
        defaultValues={defaultValues}
        onSuccess={onSuccess}
      >
        <FormInput id="anid1" name="atext" label="Name" />
        <FormInput id="anid2" name="anemailaddress" label="Email" />
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

  it('when execute fails with expected error, displays expected error', () => {
    mockAction.lastExpectedError = { code: 'A_VALIDATION_ERROR' as any };
    const expectedErrorMessages = { A_VALIDATION_ERROR: 'amessage' };

    render(
      <Form
        id="anid"
        action={mockAction}
        validationSchema={validationSchema}
        expectedErrorMessages={expectedErrorMessages}
      >
        <FormInput id="anid1" name="atext" label="Name" />
      </Form>
    );

    const expectedError = screen.getByTestId('anid_action_form_expected_error_alert');
    expect(expectedError).not.toBeNull();
    expect(expectedError.textContent).toBe('amessage');
  });

  it('when execute fails with unexpected error, displays unexpected error', () => {
    mockAction.lastUnexpectedError = new Error('anerror') as any;

    render(
      <Form id="anid" action={mockAction} validationSchema={validationSchema}>
        <FormInput id="anid1" name="atext" label="Name" />
      </Form>
    );

    const unexpectedError = screen.getByTestId('anid_action_form_unexpected_error_unhandled_error_details');
    expect(unexpectedError).not.toBeNull();
  });

  it('handles form without validations', () => {
    render(
      <Form id="anid" action={mockAction} validationSchema={undefined as any}>
        <FormInput id="anid1" name="atext" label="Name" />
      </Form>
    );

    const form = screen.getByTestId('anid_action_form');
    expect(form).not.toBeNull();
  });
});

describe('getRequiredFields', () => {
  it('when schema is undefined, returns no required', () => {
    const requiredFields = getRequiredFields(undefined);

    expect(requiredFields).toEqual([]);
  });
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
