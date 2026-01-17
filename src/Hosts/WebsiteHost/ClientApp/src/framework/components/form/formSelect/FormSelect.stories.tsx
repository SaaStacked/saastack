import type { Meta, StoryObj } from '@storybook/react';
import { within } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { z } from 'zod';
import { ActionResult } from '../../../actions/Actions.ts';
import FormAction from '../FormAction.tsx';
import FormSubmitButton from '../formSubmitButton/FormSubmitButton.tsx';
import FormSelect from './FormSelect';

const meta: Meta<typeof FormSelect> = {
  title: 'Components/Form/FormSelect',
  component: FormSelect,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    className: {
      control: 'text'
    },
    dependencies: {
      control: 'object'
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

const countryOptions = [
  { value: 'us', label: 'United States' },
  { value: 'ca', label: 'Canada' },
  { value: 'uk', label: 'United Kingdom' },
  { value: 'fr', label: 'France' },
  { value: 'de', label: 'Germany' }
];

export const Default: Story = {
  args: {
    id: 'country',
    name: 'country',
    label: 'Country',
    placeholder: 'Select your country',
    options: countryOptions
  },
  render: (args) => (
    <FormAction
      action={createMockAction()}
      defaultValues={{ country: '' }}
      validationSchema={z.object({
        country: z.string().min(1, 'Country is required').optional()
      })}
    >
      <FormSelect {...args} />
      <FormSubmitButton />
    </FormAction>
  )
};

export const WithDefaultValue: Story = {
  args: {
    id: 'country',
    name: 'country',
    label: 'Country',
    placeholder: 'Select your country',
    options: countryOptions
  },
  render: (args) => (
    <FormAction
      action={createMockAction()}
      defaultValues={{ country: 'ca' }}
      validationSchema={z.object({
        country: z.string().min(1, 'Country is required').optional()
      })}
    >
      <FormSelect {...args} />
      <FormSubmitButton />
    </FormAction>
  )
};

export const RequiredField: Story = {
  args: {
    id: 'country',
    name: 'country',
    label: 'Country',
    placeholder: 'Select your country',
    options: countryOptions
  },
  render: (args) => (
    <FormAction
      action={createMockAction()}
      defaultValues={{ country: '' }}
      validationSchema={z.object({
        country: z.string().min(1, 'Country is required')
      })}
    >
      <div className="space-y-4">
        <FormSelect {...args} />
        <FormSubmitButton />
      </div>
    </FormAction>
  )
};

export const WithValidationError: Story = {
  args: {
    id: 'country',
    name: 'country',
    label: 'Country',
    placeholder: 'Select your country',
    options: countryOptions
  },
  render: (args) => (
    <FormAction
      action={createMockAction()}
      defaultValues={{ country: '' }}
      validationSchema={z.object({
        country: z.string().min(1, 'Country is required')
      })}
    >
      <div className="space-y-4">
        <FormSelect data-testid="select" {...args} />
        <FormSubmitButton />
      </div>
    </FormAction>
  ),
  play: async ({ canvasElement }) => {
    const canvas = within(canvasElement);
    const select = canvas.getByTestId('country_form_select_select');

    // Focus and blur to trigger validation
    await userEvent.click(select);
    await userEvent.tab();
  }
};

export const LongOptions: Story = {
  args: {
    id: 'language',
    name: 'language',
    label: 'Programming Language',
    placeholder: 'Choose your favorite language',
    options: [
      { value: 'js', label: 'JavaScript - A versatile programming language' },
      { value: 'ts', label: 'TypeScript - JavaScript with static type definitions' },
      { value: 'py', label: 'Python - A high-level programming language' },
      { value: 'java', label: 'Java - A class-based, object-oriented programming language' },
      { value: 'csharp', label: 'C# - A general-purpose, multi-paradigm programming language' }
    ]
  },
  render: (args) => (
    <FormAction
      action={createMockAction()}
      validationSchema={z.object({
        language: z.string().min(1, 'Language is required').optional()
      })}
    >
      <div className="w-96">
        <FormSelect {...args} />
        <FormSubmitButton />
      </div>
    </FormAction>
  )
};

export const ManyOptions: Story = {
  args: {
    id: 'state',
    name: 'state',
    label: 'US State',
    placeholder: 'Select a state',
    options: [
      { value: 'al', label: 'Alabama' },
      { value: 'ak', label: 'Alaska' },
      { value: 'az', label: 'Arizona' },
      { value: 'ar', label: 'Arkansas' },
      { value: 'ca', label: 'California' },
      { value: 'co', label: 'Colorado' },
      { value: 'ct', label: 'Connecticut' },
      { value: 'de', label: 'Delaware' },
      { value: 'fl', label: 'Florida' },
      { value: 'ga', label: 'Georgia' },
      { value: 'hi', label: 'Hawaii' },
      { value: 'id', label: 'Idaho' },
      { value: 'il', label: 'Illinois' },
      { value: 'in', label: 'Indiana' },
      { value: 'ia', label: 'Iowa' },
      { value: 'ks', label: 'Kansas' },
      { value: 'ky', label: 'Kentucky' },
      { value: 'la', label: 'Louisiana' },
      { value: 'me', label: 'Maine' },
      { value: 'md', label: 'Maryland' },
      { value: 'ma', label: 'Massachusetts' },
      { value: 'mi', label: 'Michigan' },
      { value: 'mn', label: 'Minnesota' },
      { value: 'ms', label: 'Mississippi' },
      { value: 'mo', label: 'Missouri' },
      { value: 'mt', label: 'Montana' },
      { value: 'ne', label: 'Nebraska' },
      { value: 'nv', label: 'Nevada' },
      { value: 'nh', label: 'New Hampshire' },
      { value: 'nj', label: 'New Jersey' },
      { value: 'nm', label: 'New Mexico' },
      { value: 'ny', label: 'New York' },
      { value: 'nc', label: 'North Carolina' },
      { value: 'nd', label: 'North Dakota' },
      { value: 'oh', label: 'Ohio' },
      { value: 'ok', label: 'Oklahoma' },
      { value: 'or', label: 'Oregon' },
      { value: 'pa', label: 'Pennsylvania' },
      { value: 'ri', label: 'Rhode Island' },
      { value: 'sc', label: 'South Carolina' },
      { value: 'sd', label: 'South Dakota' },
      { value: 'tn', label: 'Tennessee' },
      { value: 'tx', label: 'Texas' },
      { value: 'ut', label: 'Utah' },
      { value: 'vt', label: 'Vermont' },
      { value: 'va', label: 'Virginia' },
      { value: 'wa', label: 'Washington' },
      { value: 'wv', label: 'West Virginia' },
      { value: 'wi', label: 'Wisconsin' },
      { value: 'wy', label: 'Wyoming' }
    ]
  },
  render: (args) => (
    <FormAction
      action={createMockAction()}
      validationSchema={z.object({
        state: z.string().min(1, 'State is required').optional()
      })}
    >
      <FormSelect {...args} />
      <FormSubmitButton />
    </FormAction>
  )
};
