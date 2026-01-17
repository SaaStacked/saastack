import type { Meta, StoryObj } from '@storybook/react';
import Input from './Input';

const meta: Meta<typeof Input> = {
  title: 'Components/Input',
  component: Input,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    type: {
      control: { type: 'select' },
      options: ['text', 'email', 'password', 'number', 'tel', 'url']
    },
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

export const Default: Story = {
  args: {
    label: 'A Label',
    placeholder: 'Enter text...',
    size: 'md'
  }
};

export const WithLabel: Story = {
  args: {
    label: 'Email Address',
    placeholder: 'Enter your email',
    type: 'email'
  }
};

export const Required: Story = {
  args: {
    label: 'Full Name',
    placeholder: 'Enter your full name',
    required: true
  }
};

export const WithHelpText: Story = {
  args: {
    label: 'Password',
    type: 'password',
    placeholder: 'Enter password',
    hintText: 'Must be at least 8 characters long'
  }
};

export const WithValidationError: Story = {
  args: {
    label: 'Email Address',
    type: 'email',
    placeholder: 'Enter your email',
    errorMessage: 'Please enter a valid email address',
    value: 'invalid-email'
  }
};

export const Disabled: Story = {
  args: {
    label: 'Disabled Input',
    placeholder: 'This input is disabled',
    disabled: true,
    value: 'Disabled value'
  }
};

export const Small: Story = {
  args: {
    label: 'Small Input',
    placeholder: 'Small size',
    size: 'sm'
  }
};

export const Large: Story = {
  args: {
    label: 'Large Input',
    placeholder: 'Large size',
    size: 'lg'
  }
};

export const FullWidth: Story = {
  args: {
    label: 'Full Width Input',
    placeholder: 'This input takes full width',
    fullWidth: true
  }
};

export const AllTypes: Story = {
  render: () => (
    <div className="space-y-4 w-80">
      <Input label="Text" type="text" placeholder="Text input" />
      <Input label="Email" type="email" placeholder="Email input" />
      <Input label="Password" type="password" placeholder="Password input" />
      <Input label="Number" type="number" placeholder="Number input" />
      <Input label="Telephone" type="tel" placeholder="Phone number" />
      <Input label="URL" type="url" placeholder="Website URL" />
    </div>
  )
};
