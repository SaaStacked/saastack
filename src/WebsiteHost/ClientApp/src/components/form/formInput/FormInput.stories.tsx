import type { Meta, StoryObj } from '@storybook/react';
import { within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { z } from 'zod';
import { ActionResult } from '../../../actions/Actions.ts';
import Form from '../Form.tsx';
import FormSubmitButton from '../formSubmitButton/FormSubmitButton.tsx';
import FormInput from './FormInput';


const meta: Meta<typeof FormInput> = {
  title: 'Components/Form/FormInput',
  component: FormInput,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    type: {
      control: 'select',
      options: ['text', 'email', 'password', 'number']
    },
    autoComplete: {
      control: 'select',
      options: ['username', 'current-password', 'new-password']
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

const createMockAction = (overrides: Partial<ActionResult<any, any, any>> = {}): ActionResult<any, any, any> => {
  const mockAction = {
    execute: async () => {
      mockAction.isExecuting = true;
      await new Promise((resolve) => setTimeout(resolve, 1500));
      mockAction.isReady = true;
      mockAction.isExecuting = false;
      mockAction.isSuccess = true;
      mockAction.lastSuccessResponse = { success: true, data: {} };
      return;
    },
    isSuccess: undefined,
    lastSuccessResponse: undefined,
    lastExpectedError: undefined,
    lastUnexpectedError: undefined,
    isExecuting: false,
    isReady: true,
    lastRequestValues: undefined,
    ...overrides
  };

  return mockAction;
};

export const TextInput: Story = {
  args: {
    id: 'name',
    name: 'name',
    label: 'Full Name',
    placeholder: 'Enter your full name'
  },
  render: (args) => (
    <Form
      action={createMockAction()}
      defaultValues={{ name: 'John' }}
      validationSchema={z.object({
        name: z.string().min(1, 'Name is required').optional()
      })}
    >
      <div className="space-y-4">
        <FormInput {...args} />
        <FormSubmitButton />
      </div>
    </Form>
  )
};

export const NumberInput: Story = {
  args: {
    id: 'age',
    name: 'age',
    type: 'number',
    label: 'Age',
    placeholder: 'Enter your age'
  },
  render: (args) => (
    <Form
      action={createMockAction()}
      defaultValues={{ age: '23' }}
      validationSchema={z.object({
        age: z.number().min(18, 'Age must be more than 18').max(100, 'Age must be less than 100').optional()
      })}
    >
      <div className="space-y-4">
        <FormInput {...args} />
        <FormSubmitButton />
      </div>
    </Form>
  )
};

export const EmailInput: Story = {
  args: {
    id: 'email',
    name: 'email',
    type: 'email',
    label: 'Email Address',
    placeholder: 'Enter your email',
    autoComplete: 'username'
  },
  render: (args) => (
    <Form
      action={createMockAction()}
      defaultValues={{ email: 'john@example.com' }}
      validationSchema={z.object({
        email: z.email('Invalid email address').optional()
      })}
    >
      <div className="space-y-4">
        <FormInput {...args} />
        <FormSubmitButton />
      </div>
    </Form>
  )
};

export const PasswordInput: Story = {
  args: {
    id: 'password',
    name: 'password',
    type: 'password',
    label: 'Password',
    placeholder: 'Enter your password',
    autoComplete: 'current-password'
  },
  render: (args) => (
    <Form
      action={createMockAction()}
      defaultValues={{ password: 'apassword' }}
      validationSchema={z.object({
        password: z.string().min(8, 'Password is invalid').optional()
      })}
    >
      <div className="space-y-4">
        <FormInput {...args} />
        <FormSubmitButton />
      </div>
    </Form>
  )
};

export const RequiredField: Story = {
  args: {
    id: 'name',
    name: 'name',
    label: 'Full Name',
    placeholder: 'Enter your full name'
  },
  render: (args) => (
    <Form
      action={createMockAction()}
      defaultValues={{ name: 'John' }}
      validationSchema={z.object({
        name: z.string().min(1, 'Name is required')
      })}
    >
      <div className="space-y-4">
        <FormInput {...args} />
        <FormSubmitButton />
      </div>
    </Form>
  )
};

export const WithValidationError: Story = {
  args: {
    id: 'name',
    name: 'name',
    label: 'Full Name',
    placeholder: 'Enter your full name'
  },
  render: (args) => (
    <Form
      action={createMockAction()}
      validationSchema={z.object({
        name: z.string().min(1, 'Name is required')
      })}
    >
      <div className="space-y-4">
        <FormInput data-testid="input" {...args} />
        <FormSubmitButton />
      </div>
    </Form>
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const input = canvas.getByTestId('name_form_input_input');

    // Click in and out of the element
    await userEvent.click(input);
    await userEvent.tab();
  }
};
