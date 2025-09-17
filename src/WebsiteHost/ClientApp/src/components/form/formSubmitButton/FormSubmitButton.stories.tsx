import type { Meta, StoryObj } from '@storybook/react';
import { within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { AxiosError } from 'axios';
import { z } from 'zod';
import { ActionRequestData, ActionResult } from '../../../actions/Actions.ts';
import { ExpectedErrorDetails } from '../../../actions/ApiErrorState.ts';
import Alert from '../../alert/Alert.tsx';
import Form from '../Form.tsx';
import FormInput from '../formInput/FormInput.tsx';
import FormSubmitButton from './FormSubmitButton';


const meta: Meta<typeof FormSubmitButton> = {
  title: 'Components/Form/FormSubmitButton',
  component: FormSubmitButton,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    label: {
      control: 'text'
    },
    busyLabel: {
      control: 'text'
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

const createMockAction = <
  TRequestData extends ActionRequestData,
  TResponse = any,
  ExpectedErrorCode extends string = any
>(
  initial: { isReady: boolean; isExecuting: boolean } = { isReady: true, isExecuting: false },
  final: {
    isSuccess?: boolean;
    lastSuccessResponse?: TResponse;
    lastExpectedError?: ExpectedErrorDetails<ExpectedErrorCode>;
    lastUnexpectedError?: AxiosError;
  } = {
    isSuccess: true,
    lastSuccessResponse: { data: {} } as TResponse,
    lastExpectedError: undefined,
    lastUnexpectedError: undefined
  }
): ActionResult<TRequestData, ExpectedErrorCode, TResponse> => {
  const action = {
    execute: async (
      requestData?: any,
      options?: { onSuccess?: (params: { requestData?: TRequestData; response: TResponse }) => void }
    ) => {
      action.isExecuting = true;
      await new Promise((resolve) => setTimeout(resolve, 1500));
      action.isReady = true;
      action.isExecuting = false;
      action.isSuccess = final.isSuccess;
      action.lastSuccessResponse = final.lastSuccessResponse;
      action.lastExpectedError = final.lastExpectedError;
      action.lastUnexpectedError = final.lastUnexpectedError;
      options?.onSuccess?.({ requestData, response: action.lastSuccessResponse! });
      return;
    },
    isSuccess: undefined,
    lastSuccessResponse: undefined,
    lastExpectedError: undefined,
    lastUnexpectedError: undefined,
    isExecuting: initial.isExecuting,
    isReady: initial.isReady,
    lastRequestValues: undefined
  } as ActionResult<TRequestData, ExpectedErrorCode, TResponse>;

  return action;
};

const validations = z.object({
  name: z.string().min(1, 'Name is required'),
  email: z.email('Invalid email address')
});

export const ReadyToSubmit: Story = {
  args: {
    label: 'Submit',
    busyLabel: 'Processing...'
  },
  decorators: [
    (Story, _context) => (
      <div className="space-y-8">
        <Alert title="Note">
          <p>Once the form is submitted successfully, the 'Submit' button will remain disabled.</p>
          <p>As a safeguard to prevent the form from being submitted multiple times by the user.</p>
        </Alert>
        <Story />
      </div>
    )
  ],
  render: (args) => (
    <Form
      action={createMockAction()}
      defaultValues={{ name: 'John', email: 'john@example.com' }}
      validationSchema={validations}
    >
      <FormInput id="name" name="name" label="Name" placeholder="Enter your name" />
      <FormInput id="email" name="email" label="Email" type="email" placeholder="Enter your email" />
      <FormSubmitButton {...args} />
    </Form>
  )
};

export const Executing: Story = {
  args: {
    label: 'Submit',
    busyLabel: 'Processing...'
  },
  render: (args) => (
    <Form
      action={createMockAction({ isReady: true, isExecuting: true })}
      defaultValues={{ name: 'John', email: 'john@example.com' }}
      validationSchema={validations}
    >
      <FormInput id="name" name="name" label="Name" placeholder="Enter your name" />
      <FormInput id="email" name="email" label="Email" type="email" placeholder="Enter your email" />
      <FormSubmitButton {...args} />
    </Form>
  )
};

export const FormInvalid: Story = {
  args: {
    label: 'Submit',
    busyLabel: 'Processing...'
  },
  render: (args) => (
    <Form
      action={createMockAction()}
      defaultValues={{ name: 'John', email: 'notanemailaddress' }}
      validationSchema={validations}
    >
      <div className="space-y-4">
        <FormInput id="name" name="name" label="Name" placeholder="Enter your name" />
        <FormInput id="email" name="email" label="Email" type="email" placeholder="Enter your email" />
        <FormSubmitButton {...args} />
      </div>
    </Form>
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const input = canvas.getByTestId('email_form_input_input');

    // Click in and out of the element
    await userEvent.click(input);
    await userEvent.tab();
  }
};

export const BrowserIsOffline: Story = {
  args: {
    label: 'Submit',
    busyLabel: 'Processing...'
  },
  render: (args) => (
    <Form
      action={createMockAction({ isReady: false, isExecuting: false })}
      defaultValues={{ name: 'John', email: 'john@example.com' }}
      validationSchema={validations}
    >
      <div className="space-y-4">
        <FormInput id="name" name="name" label="Name" placeholder="Enter your name" />
        <FormInput id="email" name="email" label="Email" type="email" placeholder="Enter your email" />
        <FormSubmitButton {...args} />
      </div>
    </Form>
  )
};
