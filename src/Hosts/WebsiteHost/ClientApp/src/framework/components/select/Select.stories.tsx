import type { Meta, StoryObj } from '@storybook/react';
import Select from './Select';

const meta: Meta<typeof Select> = {
  title: 'Components/Select',
  component: Select,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    size: {
      control: { type: 'select' },
      options: ['sm', 'md', 'lg']
    },
    disabled: {
      control: { type: 'boolean' }
    },
    required: {
      control: { type: 'boolean' }
    },
    fullWidth: {
      control: { type: 'boolean' }
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

const options = [
  { value: 'option1', label: 'Option 1' },
  { value: 'option2', label: 'Option 2' },
  { value: 'option3', label: 'Option 3' },
  { value: 'option4', label: 'Option 4' }
];

const countryOptions = [
  { value: 'us', label: 'United States' },
  { value: 'ca', label: 'Canada' },
  { value: 'uk', label: 'United Kingdom' },
  { value: 'fr', label: 'France' },
  { value: 'de', label: 'Germany' }
];
export const Default: Story = {
  args: {
    label: 'Choose an option',
    placeholder: 'Select an option...',
    options,
    size: 'md',
    value: ''
  }
};

export const WithValue: Story = {
  args: {
    label: 'Country',
    options: countryOptions,
    value: 'ca'
  }
};

export const Required: Story = {
  args: {
    label: 'Country',
    options: countryOptions,
    required: true,
    placeholder: 'Select an option...',
    value: ''
  }
};

export const WithHelpText: Story = {
  args: {
    label: 'Country',
    options: countryOptions,
    hintText: 'Choose your country of residence',
    placeholder: 'Select an option...'
  }
};

export const WithValidationError: Story = {
  args: {
    label: 'Country',
    options: countryOptions,
    placeholder: 'Select an option...',
    errorMessage: 'Please select a country',
    value: ''
  }
};

export const Disabled: Story = {
  args: {
    label: 'Country',
    options: countryOptions,
    disabled: true,
    value: 'us'
  }
};

export const Small: Story = {
  args: {
    label: 'Country',
    options: countryOptions,
    size: 'sm',
    placeholder: 'Select an option...'
  }
};

export const Large: Story = {
  args: {
    label: 'Country',
    options: countryOptions,
    size: 'lg',
    placeholder: 'Select an option...'
  }
};

export const FullWidth: Story = {
  args: {
    label: 'Country',
    options: countryOptions,
    fullWidth: true,
    placeholder: 'Select an option...'
  }
};

export const LongOptions: Story = {
  args: {
    label: 'Programming Language',
    options: [
      { value: 'js', label: 'JavaScript - A versatile programming language' },
      { value: 'ts', label: 'TypeScript - JavaScript with static type definitions' },
      { value: 'py', label: 'Python - A high-level programming language' },
      { value: 'java', label: 'Java - A class-based, object-oriented programming language' },
      { value: 'csharp', label: 'C# - A general-purpose, multi-paradigm programming language' }
    ],
    placeholder: 'Choose your favorite language...'
  }
};

export const ManyOptions: Story = {
  args: {
    label: 'US State',
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
    ],
    placeholder: 'Select a state...'
  }
};
