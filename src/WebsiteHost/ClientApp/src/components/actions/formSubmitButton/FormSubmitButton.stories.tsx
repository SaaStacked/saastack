import type { Meta, StoryObj } from '@storybook/react';
import { FormProvider, useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { ActionResult } from '../../../actions/Actions';
import { ActionContext } from '../Contexts';
import FormSubmitButton from './FormSubmitButton';

const meta: Meta<typeof FormSubmitButton> = {
  title: 'Components/Actions/FormSubmitButton',
  component: FormSubmitButton,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    id: {
      control: 'text'
    },
    label: {
      control: 'text'
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

// Mock action for stories
const createMockAction = (overrides: Partial<ActionResult<any, any, any>> = {}): ActionResult<any, any, any> => ({
  execute: () => Promise.resolve(),
  isSuccess: false,
  lastSuccessResponse: undefined,
  lastExpectedError: undefined,
  lastUnexpectedError: undefined,
  isExecuting: false,
  isReady: true,
  lastRequestValues: undefined,
  ...overrides
});

const FormWrapper = ({
  children,
  action,
  defaultValues = {},
  isValid = true
}: {
  children: React.ReactNode;
  action: ActionResult<any, any, any>;
  defaultValues?: any;
  isValid?: boolean;
}) => {
  const schema = z.object({
    name: z.string().min(1, 'Name is required'),
    email: z.email('Invalid email address')
  });

  const methods = useForm({
    resolver: zodResolver(schema),
    defaultValues,
    mode: 'onBlur'
  });

  // Simulate form validation state
  if (!isValid) {
    methods.setError('name', { message: 'Name is required' });
  }

  return (
    <ActionContext.Provider value={action}>
      <FormProvider {...methods}>
        <form className="p-4 space-y-4 bg-white rounded-lg border">
          <div>
            <label htmlFor="name" className="block text-sm font-medium text-gray-700">
              Name
            </label>
            <input
              {...methods.register('name')}
              type="text"
              id="name"
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm"
            />
          </div>
          <div>
            <label htmlFor="email" className="block text-sm font-medium text-gray-700">
              Email Address
            </label>
            <input
              {...methods.register('email')}
              type="email"
              id="email"
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm"
            />
          </div>
          {children}
        </form>
      </FormProvider>
    </ActionContext.Provider>
  );
};

export const Default: Story = {
  args: {
    id: 'submit',
    label: 'Submit'
  },
  render: (args) => (
    <FormWrapper action={createMockAction()}>
      <FormSubmitButton {...args} />
    </FormWrapper>
  )
};

export const Executing: Story = {
  args: {
    id: 'submit',
    label: 'Processing...'
  },
  render: (args) => (
    <FormWrapper action={createMockAction({ isExecuting: true })}>
      <FormSubmitButton {...args} />
    </FormWrapper>
  )
};

export const ActionNotReady: Story = {
  args: {
    id: 'submit',
    label: 'Submit'
  },
  render: (args) => (
    <FormWrapper action={createMockAction({ isReady: false })}>
      <FormSubmitButton {...args} />
    </FormWrapper>
  )
};

export const FormInvalid: Story = {
  args: {
    id: 'submit',
    label: 'Submit'
  },
  render: (args) => (
    <FormWrapper action={createMockAction()} isValid={false} defaultValues={{ name: '', email: 'invalid-email' }}>
      <FormSubmitButton {...args} />
    </FormWrapper>
  )
};
