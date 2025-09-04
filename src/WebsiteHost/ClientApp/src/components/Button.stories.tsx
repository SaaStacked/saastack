import type { Meta, StoryObj } from '@storybook/react';
import Button from './Button';

const meta: Meta<typeof Button> = {
  title: 'Components/Button',
  component: Button,
  parameters: {
    layout: 'centered'
  },
  tags: ['autodocs'],
  argTypes: {
    variant: {
      control: { type: 'select' },
      options: ['primary', 'secondary', 'outline', 'ghost', 'danger']
    },
    size: {
      control: { type: 'select' },
      options: ['sm', 'md', 'lg']
    },
    disabled: {
      control: { type: 'boolean' }
    },
    loading: {
      control: { type: 'boolean' }
    },
    fullWidth: {
      control: { type: 'boolean' }
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

export const Primary: Story = {
  args: {
    children: 'Button',
    variant: 'primary'
  }
};

export const Secondary: Story = {
  args: {
    children: 'Button',
    variant: 'secondary'
  }
};

export const Outline: Story = {
  args: {
    children: 'Button',
    variant: 'outline'
  }
};

export const Ghost: Story = {
  args: {
    children: 'Button',
    variant: 'ghost'
  }
};

export const Danger: Story = {
  args: {
    children: 'Button',
    variant: 'danger'
  }
};

export const Small: Story = {
  args: {
    children: 'Button',
    size: 'sm'
  }
};

export const Large: Story = {
  args: {
    children: 'Button',
    size: 'lg'
  }
};

export const Loading: Story = {
  args: {
    children: 'Button',
    loading: true
  }
};

export const Disabled: Story = {
  args: {
    children: 'Button',
    disabled: true
  }
};

export const FullWidth: Story = {
  args: {
    children: 'Button',
    fullWidth: true
  },
  parameters: {
    layout: 'padded'
  }
};

export const AllVariants: Story = {
  render: () => (
    <div className="space-y-4">
      <div className="space-x-2">
        <Button variant="primary">Primary</Button>
        <Button variant="secondary">Secondary</Button>
        <Button variant="outline">Outline</Button>
        <Button variant="ghost">Ghost</Button>
        <Button variant="danger">Danger</Button>
      </div>
      <div className="space-x-2">
        <Button size="sm">Small</Button>
        <Button size="md">Medium</Button>
        <Button size="lg">Large</Button>
      </div>
      <div className="space-x-2">
        <Button loading>Loading</Button>
        <Button disabled>Disabled</Button>
      </div>
    </div>
  ),
  parameters: {
    layout: 'padded'
  }
};
