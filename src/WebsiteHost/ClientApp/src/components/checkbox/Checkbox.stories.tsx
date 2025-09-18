import type { Meta, StoryObj } from '@storybook/react';
import Checkbox from './Checkbox';

const meta: Meta<typeof Checkbox> = {
  title: 'Components/Checkbox',
  component: Checkbox,
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
    fullWidth: {
      control: { type: 'boolean' }
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Default: Story = {
  args: {
    label: 'I agree to the terms and conditions',
    size: 'md'
  }
};

export const WithValidationError: Story = {
  args: {
    label: 'I agree to the terms and conditions',
    errorMessage: 'You must agree to the terms to continue'
  }
};

export const Disabled: Story = {
  render: () => (
    <div className="space-y-4">
      <Checkbox label="Disabled unchecked" disabled value={false} />
      <Checkbox label="Disabled checked" disabled value={true} />
    </div>
  )
};

export const Small: Story = {
  args: {
    label: 'I agree to the terms and conditions',
    size: 'sm'
  }
};

export const Large: Story = {
  args: {
    label: 'I agree to the terms and conditions',
    size: 'lg'
  }
};

export const FullWidth: Story = {
  args: {
    label: 'I agree to the terms and conditions',
    fullWidth: true
  }
};
