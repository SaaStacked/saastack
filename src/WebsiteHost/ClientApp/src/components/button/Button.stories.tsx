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
    label: {
      control: 'text'
    },
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
    busy: {
      control: { type: 'boolean' }
    },
    fullWidth: {
      control: { type: 'boolean' }
    }
  }
};

export default meta;
type Story = StoryObj<typeof meta>;

export const AllVariants: Story = {
  render: () => (
    <div className="grid grid-cols-3 gap-16 max-w-2xl">
      <Button variant="primary">Primary</Button>
      <Button variant="secondary">Secondary</Button>
      <Button variant="outline">Outline</Button>
      <Button variant="ghost">Ghost</Button>
      <Button variant="danger">Danger</Button>
      <Button size="sm">Small</Button>
      <Button size="md">Medium</Button>
      <Button size="lg">Large</Button>
      <Button busy>Loading</Button>
      <Button disabled>Disabled</Button>
    </div>
  ),
  parameters: {
    layout: 'centered'
  }
};

export const Primary: Story = {
  args: {
    label: 'Button',
    variant: 'primary'
  }
};

export const Secondary: Story = {
  args: {
    label: 'Button',
    variant: 'secondary'
  }
};

export const Outline: Story = {
  args: {
    label: 'Button',
    variant: 'outline'
  }
};

export const Ghost: Story = {
  args: {
    label: 'Button',
    variant: 'ghost'
  }
};

export const Danger: Story = {
  args: {
    label: 'Button',
    variant: 'danger'
  }
};

export const Small: Story = {
  args: {
    label: 'Button',
    size: 'sm'
  }
};

export const Large: Story = {
  args: {
    label: 'Button',
    size: 'lg'
  }
};

export const Loading: Story = {
  args: {
    label: 'Button',
    busy: true
  }
};

export const Disabled: Story = {
  args: {
    label: 'Button',
    disabled: true
  }
};

export const FullWidth: Story = {
  args: {
    label: 'Button',
    fullWidth: true
  },
  parameters: {
    layout: 'centered'
  }
};
