import type { Meta, StoryObj } from '@storybook/react';
import { toast } from 'react-toastify';
import { AxiosError } from 'axios';
import { z } from 'zod';
import { ActionRequestData, ActionResult } from '../../actions/Actions.ts';
import { ExpectedErrorDetails } from '../../actions/ApiErrorState.ts';
import { IOfflineService } from '../../services/IOfflineService.ts';
import { OfflineServiceContext } from '../../services/OfflineServiceContext.tsx';
import Alert from '../alert/Alert.tsx';
import { OfflineBanner } from '../offline/OfflineBanner.tsx';
import Form from './Form';
import FormInput from './formInput/FormInput.tsx';
import FormSubmitButton from './formSubmitButton/FormSubmitButton';


const meta: Meta<typeof Form> = {
  title: 'Components/Form/Form',
  component: Form,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    className: {
      control: 'text'
    },
    validatesWhen: {
      control: { type: 'select' },
      options: ['onBlur', 'onChange', 'all', 'onTouched', 'all']
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

const mockOfflineService = {
  status: 'offline',
  onStatusChanged(_: (status: 'online' | 'offline') => void): () => void {
    return () => {};
  }
} as IOfflineService;

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

const validationSchema = z.object({
  firstName: z.string().min(1, 'FirstName is required'),
  emailAddress: z.email('Please enter a valid email address'),
  message: z.string().optional()
});

export const ReturnsSuccess: Story = {
  args: {
    action: createMockAction(),
    validationSchema,
    validatesWhen: 'all',
    defaultValues: {
      firstName: 'John Doe',
      emailAddress: 'john.doe@example.com'
    },
    onSuccess: (_params) => toast.success(`API call succeeded: HTTP - OK`, { autoClose: 1500 })
  },
  decorators: [
    (Story, _context) => (
      <div className="space-y-8">
        <Alert title="Note">
          <p>
            Once the form is submitted successfully, the 'Submit' button, and all form controls will remain disabled.
          </p>
          <p>This is a safeguard to prevent the form from being submitted multiple times by the user.</p>
        </Alert>
        <Story />
      </div>
    )
  ],
  render: (args) => (
    <Form {...args}>
      <FormInput id="firstName" name="firstName" label="First Name" placeholder="Enter your first name" />
      <FormInput
        id="emailAddress"
        name="emailAddress"
        type="email"
        label="Email Address"
        placeholder="Enter your email"
        autoComplete="username"
      />
      <FormSubmitButton label="Submit" />
    </Form>
  )
};

export const ReturnsExpectedError: Story = {
  args: {
    action: createMockAction(
      { isReady: true, isExecuting: false },
      {
        isSuccess: false,
        lastExpectedError: { code: 'ANERROR' }
      }
    ),
    validationSchema,
    validatesWhen: 'all',
    defaultValues: {
      firstName: 'John Doe',
      emailAddress: 'john.doe@example.com'
    },
    expectedErrorMessages: {
      ANERROR: 'Cannot perform this action at this time'
    },
    onSuccess: (_params) => toast.error(`API call failed: HTTP - BadRequest`, { autoClose: 1500 })
  },
  decorators: [
    (Story, _context) => (
      <div className="space-y-8">
        <Alert title="Note">
          <p>When this form is submitted, an 'HTTP - 405' is returned.</p>
          <p>This error is expected, and a custom UI message is displayed to the user.</p>
          <p>Note: The form can still be edited and re-submitted.</p>
        </Alert>
        <Story />
      </div>
    )
  ],
  render: (args) => (
    <Form {...args}>
      <FormInput id="firstName" name="firstName" label="First Name" placeholder="Enter your first name" />
      <FormInput
        id="emailAddress"
        name="emailAddress"
        type="email"
        label="Email Address"
        placeholder="Enter your email"
        autoComplete="username"
      />
      <FormSubmitButton label="Submit" />
    </Form>
  )
};

export const ReturnsUnexpectedError: Story = {
  args: {
    action: createMockAction(
      { isReady: true, isExecuting: false },
      {
        isSuccess: false,
        lastExpectedError: undefined,
        lastUnexpectedError: {
          isAxiosError: true,
          message: 'BadRequest',
          response: {
            status: 400,
            statusText: 'BadRequest',
            data: {
              type: 'https://datatracker.ietf.org/doc/html/rfc9110#section-15.5',
              title: 'Bad Request',
              status: 400,
              detail: "'First Name' must not be empty.",
              instance: 'https://localhost:5001/credentials/register',
              errors: [
                {
                  reason: "'First Name' must not be empty.",
                  rule: 'NotEmptyValidator',
                  value: ''
                },
                {
                  reason: "The 'FirstName' was either missing or is invalid",
                  rule: 'ValidatorValidator',
                  value: ''
                }
              ]
            },
            headers: {},
            config: {} as any
          }
        } as AxiosError
      }
    ),
    validationSchema,
    validatesWhen: 'all',
    defaultValues: {
      firstName: 'John Doe',
      emailAddress: 'john.doe@example.com'
    },
    onSuccess: (_params) => toast.error(`API call failed: HTTP - BadRequest`, { autoClose: 1500 })
  },
  decorators: [
    (Story, _context) => (
      <div className="space-y-8">
        <Alert title="Note">
          <p>When this form is submitted, an 'HTTP - 500' is returned.</p>
          <p>This error would never be expected, and a general fatal error is displayed.</p>
          <p>Note: The form can still be edited and re-submitted.</p>
        </Alert>
        <Story />
      </div>
    )
  ],
  render: (args) => (
    <Form {...args}>
      <FormInput id="firstName" name="firstName" label="First Name" placeholder="Enter your first name" />
      <FormInput
        id="emailAddress"
        name="emailAddress"
        type="email"
        label="Email Address"
        placeholder="Enter your email"
        autoComplete="username"
      />
      <FormSubmitButton label="Submit" />
    </Form>
  )
};

export const ValidateOnChange: Story = {
  args: {
    action: createMockAction(),
    validationSchema,
    validatesWhen: 'all',
    onSuccess: (_params) => toast.success(`API call succeeded: HTTP - OK`, { autoClose: 1500 })
  },
  render: (args) => (
    <Form {...args}>
      <FormInput id="firstName" name="firstName" label="First Name" placeholder="Enter your first name" />
      <FormInput
        id="emailAddress"
        name="emailAddress"
        type="email"
        label="Email Address"
        placeholder="Enter your email"
        autoComplete="username"
      />
      <FormSubmitButton label="Submit" />
    </Form>
  )
};

export const ActionExecuting: Story = {
  args: {
    action: createMockAction(
      { isReady: true, isExecuting: true },
      {
        isSuccess: false
      }
    ),
    validationSchema,
    validatesWhen: 'all',
    onSuccess: (_params) => toast.error(`Should never have seen this!`, { autoClose: 1500 })
  },
  render: (args) => (
    <Form {...args}>
      <FormInput id="firstName" name="firstName" label="First Name" placeholder="Enter your first name" />
      <FormInput
        id="emailAddress"
        name="emailAddress"
        type="email"
        label="Email Address"
        placeholder="Enter your email"
        autoComplete="username"
      />
      <FormSubmitButton label="Submit" />
    </Form>
  )
};

export const BrowserIsOffline: Story = {
  args: {
    action: createMockAction({ isReady: false, isExecuting: false }, {}),
    validationSchema,
    validatesWhen: 'all',
    onSuccess: (_params) => toast.error(`Should never have seen this!`, { autoClose: 1500 })
  },
  decorators: [
    (Story, _context) => (
      <OfflineServiceContext value={{ offlineService: mockOfflineService }}>
        <OfflineBanner />
        <div className="mt-32"></div>
        <Story />
      </OfflineServiceContext>
    )
  ],
  render: (args) => (
    <Form {...args}>
      <FormInput id="firstName" name="firstName" label="First Name" placeholder="Enter your first name" />
      <FormInput
        id="emailAddress"
        name="emailAddress"
        type="email"
        label="Email Address"
        placeholder="Enter your email"
        autoComplete="username"
      />
      <FormSubmitButton label="Submit" />
    </Form>
  )
};
