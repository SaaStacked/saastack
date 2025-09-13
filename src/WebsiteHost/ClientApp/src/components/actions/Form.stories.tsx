import type { Meta, StoryObj } from '@storybook/react';
import { useFormContext } from 'react-hook-form';
import { AxiosError } from 'axios';
import { z } from 'zod';
import { ActionResult } from '../../actions/Actions';
import Form from './Form';
import FormSubmitButton from './formSubmitButton/FormSubmitButton';

const meta: Meta<typeof Form> = {
  title: 'Components/Actions/Form',
  component: Form,
  parameters: {
    layout: 'padded'
  },
  tags: ['autodocs'],
  argTypes: {
    id: {
      control: 'text'
    },
    className: {
      control: 'text'
    },
    validatesWhen: {
      control: { type: 'select' },
      options: ['onBlur', 'onChange', 'onSubmit', 'onTouched', 'all']
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

// Mock action for stories
const createMockAction = (overrides: Partial<ActionResult<any, any, any>> = {}): ActionResult<any, any, any> => ({
  execute: (formData, options) => {
    console.log('Form submitted with data:', formData);
    setTimeout(() => options?.onSuccess?.({ requestData: formData, response: { success: true } }), 1000);
    return Promise.resolve();
  },
  isSuccess: false,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined,
  ...overrides
});

// Form inputs component
const FormInputs = () => {
  const {
    register,
    formState: { errors }
  } = useFormContext();

  return (
    <div className="space-y-4">
      <div>
        <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
          Name *
        </label>
        <input
          {...register('firstName')}
          type="text"
          id="firstName"
          name="firstName"
          className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          placeholder="Enter your name"
        />
        {errors.name && <p className="mt-1 text-sm text-red-600">{errors.name.message as string}</p>}
      </div>

      <div>
        <label htmlFor="emailAddress" className="block text-sm font-medium text-gray-700 mb-1">
          Email Address *
        </label>
        <input
          {...register('email')}
          type="email"
          id="emailAddress"
          name="emailAddress"
          className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          placeholder="Enter your email address"
        />
        {errors.email && <p className="mt-1 text-sm text-red-600">{errors.email.message as string}</p>}
      </div>

      <div>
        <label htmlFor="message" className="block text-sm font-medium text-gray-700 mb-1">
          Message
        </label>
        <textarea
          {...register('message')}
          id="message"
          rows={4}
          className="w-full px-3 py-2 border border-gray-300 rounded-md shadow-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
          placeholder="Enter your message (optional)"
        />
      </div>

      <FormSubmitButton id="submit" label="Submit Form" />
    </div>
  );
};

const validationSchema = z.object({
  firstName: z.string().min(1, 'FirstName is required'),
  emailAddress: z.email('Please enter a valid email address'),
  message: z.string().optional()
});

export const Default: Story = {
  args: {
    id: 'aform',
    action: createMockAction(),
    validations: validationSchema,
    onSuccess: (params) => {
      console.log('Form submission successful:', params);
      alert('Form submitted successfully!');
    }
  },
  render: (args) => (
    <Form {...args}>
      <FormInputs />
    </Form>
  )
};

export const WithDefaultValues: Story = {
  args: {
    id: 'aform',
    action: createMockAction(),
    validations: validationSchema,
    defaultValues: {
      firstName: 'John Doe',
      emailAddress: 'john.doe@example.com',
      message: 'Hello, this is a pre-filled message!'
    },
    onSuccess: (params) => {
      console.log('Form submission successful:', params);
      alert('Form submitted successfully!');
    }
  },
  render: (args) => (
    <Form {...args}>
      <FormInputs />
    </Form>
  )
};

export const ValidateOnChange: Story = {
  args: {
    id: 'aform',
    action: createMockAction(),
    validations: validationSchema,
    validatesWhen: 'onChange',
    onSuccess: (params) => {
      console.log('Form submission successful:', params);
      alert('Form submitted successfully!');
    }
  },
  render: (args) => (
    <Form {...args}>
      <FormInputs />
    </Form>
  )
};

export const WithExpectedError: Story = {
  args: {
    id: 'aform',
    action: createMockAction({
      isExecuting: false,
      isReady: true,
      isSuccess: false,
      lastExpectedError: { code: 'VALIDATION_ERROR' }
    }),
    validations: validationSchema,
    expectedErrorMessages: {
      VALIDATION_ERROR: 'Please check your input and try again.'
    },
    onSuccess: (params) => {
      console.log('Form submission successful:', params);
      alert('Form submitted successfully!');
    }
  },
  render: (args) => (
    <Form {...args}>
      <FormInputs />
    </Form>
  )
};

export const WithUnexpectedError: Story = {
  args: {
    id: 'aform',
    action: createMockAction({
      execute(
        _requestData: any,
        _options:
          | {
              onSuccess?: (params: { requestData?: any; response: any }) => void;
            }
          | undefined
      ): void {},
      isExecuting: false,
      isReady: true,
      isSuccess: false,
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
    }),
    validations: validationSchema,
    onSuccess: (params) => {
      console.log('Form submission successful:', params);
      alert('Form submitted successfully!');
    }
  },
  render: (args) => (
    <Form {...args}>
      <FormInputs />
    </Form>
  )
};

export const ActionExecuting: Story = {
  args: {
    id: 'aform',
    action: createMockAction({
      isExecuting: true,
      isReady: true,
      isSuccess: false
    }),
    validations: validationSchema,
    onSuccess: (params) => {
      console.log('Form submission successful:', params);
      alert('Form submitted successfully!');
    }
  },
  render: (args) => (
    <Form {...args}>
      <FormInputs />
    </Form>
  )
};

export const ActionNotReady: Story = {
  args: {
    id: 'aform',
    action: createMockAction({
      isReady: false
    }),
    validations: validationSchema,
    onSuccess: (params) => {
      console.log('Form submission successful:', params);
      alert('Form submitted successfully!');
    }
  },
  render: (args) => (
    <Form {...args}>
      <FormInputs />
    </Form>
  )
};
